using System;
using System.IO;
using System.Text;
using Akka.Actor;
using ProtoBuf.Meta;
using TypeAlias;

namespace Akka.Interfaced.ProtobufSerializer
{
    public class ProtobufSerializer : Akka.Serialization.Serializer
    {
        private TypeModel _typeModel;
        private TypeAliasTable _typeTable;

        public ProtobufSerializer(ExtendedActorSystem system)
            : base(system)
        {
            _typeModel = CreateTypeModel();
            _typeTable = new TypeAliasTable();
        }

        public static TypeModel CreateTypeModel()
        {
            RuntimeTypeModel typeModel = RuntimeTypeModel.Create();
            AkkaSurrogate.Register(typeModel);
            AutoSurrogate.Register(typeModel);
            return typeModel;
        }

        public TypeModel TypeModel
        {
            get { return _typeModel; }
        }

        public TypeAliasTable TypeAliasTable
        {
            get { return _typeTable; }
        }

        public override int Identifier
        {
            get { return 10; }
        }

        public override bool IncludeManifest
        {
            // Don't use built-in manifest for saving bytes.
            get { return false; }
        }

        // IDEA: We can inject special flag when RequestId = -1 or 0
        private enum MessageCode : byte
        {
            Notification = 1,
            Request = 2,
            ReplyWithNothing = 3,
            ReplyWithException = 4,
            ReplyWithResult = 5,
        }

        public override byte[] ToBinary(object obj)
        {
            // NotificationMessage

            var notificationMessage = obj as NotificationMessage;
            if (notificationMessage != null)
            {
                using (var ms = new MemoryStream())
                {
                    // write code, observerId and notificationId

                    ms.WriteByte((byte)MessageCode.Notification);
                    ms.Write7BitEncodedInt(notificationMessage.ObserverId);
                    ms.Write7BitEncodedInt(notificationMessage.NotificationId);

                    // write message

                    WriteType(ms, notificationMessage.InvokePayload.GetType());
                    try
                    {
                        AkkaSurrogate.CurrentSystem = system;
                        _typeModel.Serialize(ms, notificationMessage.InvokePayload);
                    }
                    finally
                    {
                        AkkaSurrogate.CurrentSystem = null;
                    }

                    return ms.ToArray();
                }
            }

            // RequestMessage

            var requestMessage = obj as RequestMessage;
            if (requestMessage != null)
            {
                using (var ms = new MemoryStream())
                {
                    // write code & requestId

                    ms.WriteByte((byte)MessageCode.Request);
                    ms.Write7BitEncodedInt(requestMessage.RequestId);

                    // write message

                    WriteType(ms, requestMessage.InvokePayload.GetType());
                    try
                    {
                        AkkaSurrogate.CurrentSystem = system;
                        _typeModel.Serialize(ms, requestMessage.InvokePayload);
                    }
                    finally
                    {
                        AkkaSurrogate.CurrentSystem = null;
                    }

                    return ms.ToArray();
                }
            }

            // ResponseMessage

            var responseMessage = obj as ResponseMessage;
            if (responseMessage != null)
            {
                using (var ms = new MemoryStream())
                {
                    if (responseMessage.Exception == null && responseMessage.ReturnPayload == null)
                    {
                        ms.WriteByte((byte)MessageCode.ReplyWithNothing);
                        ms.Write7BitEncodedInt(responseMessage.RequestId);
                    }
                    else if (responseMessage.Exception != null)
                    {
                        ms.WriteByte((byte)MessageCode.ReplyWithException);
                        ms.Write7BitEncodedInt(responseMessage.RequestId);

                        var exceptionType = responseMessage.Exception.GetType();
                        WriteType(ms, exceptionType);
                        if (_typeModel.CanSerialize(exceptionType))
                            _typeModel.Serialize(ms, responseMessage.Exception);
                    }
                    else
                    {
                        ms.WriteByte((byte)MessageCode.ReplyWithResult);
                        ms.Write7BitEncodedInt(responseMessage.RequestId);

                        // write result

                        WriteType(ms, responseMessage.ReturnPayload.GetType());
                        try
                        {
                            AkkaSurrogate.CurrentSystem = system;
                            _typeModel.Serialize(ms, responseMessage.ReturnPayload);
                        }
                        finally
                        {
                            AkkaSurrogate.CurrentSystem = null;
                        }
                    }
                    return ms.ToArray();
                }
            }

            throw new InvalidOperationException(
                "ProtobufSerializer supports only NotificationMessage, RequestMessage and ResponseMessage.");
        }

        public override object FromBinary(byte[] bytes, Type type)
        {
            using (var ms = new MemoryStream(bytes))
            {
                var code = (MessageCode)ms.ReadByte();
                switch (code)
                {
                    case MessageCode.Notification:
                    {
                        // read observerId

                        var notificationMessage = new NotificationMessage();
                        notificationMessage.ObserverId = ms.Read7BitEncodedInt();
                        notificationMessage.NotificationId = ms.Read7BitEncodedInt();

                        // read message

                        var messageType = ReadType(ms);
                        if (messageType == null)
                            throw new Exception("Cannot resolve messageType.");

                        var message = Activator.CreateInstance(messageType);
                        try
                        {
                            AkkaSurrogate.CurrentSystem = system;
                            notificationMessage.InvokePayload = (IInvokable)_typeModel.Deserialize(ms, message, messageType);
                        }
                        finally
                        {
                            AkkaSurrogate.CurrentSystem = null;
                        }
                        return notificationMessage;
                    }
                    case MessageCode.Request:
                    {
                        // read requestId

                        var requestMessage = new RequestMessage();
                        requestMessage.RequestId = ms.Read7BitEncodedInt();

                        // read message

                        var messageType = ReadType(ms);
                        if (messageType == null)
                            throw new Exception("Cannot resolve messageType.");

                        var message = Activator.CreateInstance(messageType);
                        try
                        {
                            AkkaSurrogate.CurrentSystem = system;
                            requestMessage.InvokePayload = (IAsyncInvokable)_typeModel.Deserialize(ms, message, messageType);
                        }
                        finally
                        {
                            AkkaSurrogate.CurrentSystem = null;
                        }
                        return requestMessage;
                    }
                    case MessageCode.ReplyWithNothing:
                    {
                        var replyMessage = new ResponseMessage();
                        replyMessage.RequestId = ms.Read7BitEncodedInt();
                        return replyMessage;
                    }
                    case MessageCode.ReplyWithException:
                    {
                        var replyMessage = new ResponseMessage();
                        replyMessage.RequestId = ms.Read7BitEncodedInt();

                        var exceptionType = ReadType(ms);
                        replyMessage.Exception = (Exception)Activator.CreateInstance(exceptionType);
                        if (_typeModel.CanSerialize(exceptionType))
                            _typeModel.Deserialize(ms, replyMessage.Exception, exceptionType);
                            return replyMessage;
                    }
                    case MessageCode.ReplyWithResult:
                    {
                        // read requestId

                        var replyMessage = new ResponseMessage();
                        replyMessage.RequestId = ms.Read7BitEncodedInt();

                        // read result

                        var resultType = ReadType(ms);
                        if (resultType == null)
                            throw new Exception("Cannot resolve resultType.");

                        var result = Activator.CreateInstance(resultType);
                        try
                        {
                            AkkaSurrogate.CurrentSystem = system;
                            replyMessage.ReturnPayload = (IValueGetable)_typeModel.Deserialize(ms, result, resultType);
                        }
                        finally
                        {
                            AkkaSurrogate.CurrentSystem = null;
                        }
                        return replyMessage;
                    }
                    default:
                    {
                        return null;
                    }
                }
            }
        }

        private void WriteType(Stream stream, Type type)
        {
            var messageTypeAlias = _typeTable.GetAlias(type);
            if (messageTypeAlias != 0)
            {
                // Write big endian
                var bytes = BitConverter.GetBytes(messageTypeAlias);
                stream.WriteByte(bytes[3]);
                stream.WriteByte(bytes[2]);
                stream.WriteByte(bytes[1]);
                stream.WriteByte(bytes[0]);
            }
            else
            {
                // Write string with length 0x80 for making msb of first byte set
                var name = type.AssemblyQualifiedName;
                var bytes = Encoding.UTF8.GetBytes(name);
                stream.Write7BitEncodedInt(0x80 + bytes.Length);
                stream.Write(bytes, 0, bytes.Length);
            }
        }

        private Type ReadType(Stream stream)
        {
            var firstByte = stream.ReadByte();
            if ((firstByte & 0x80) == 0)
            {
                // use type alias
                var b1 = stream.ReadByte();
                var b2 = stream.ReadByte();
                var b3 = stream.ReadByte();
                var typeAlias = ((int)(firstByte) << 24) | ((int)(b1) << 16) | ((int)(b2) << 8) | ((int)(b3));
                return _typeTable.GetType(typeAlias);
            }
            else
            {
                // use type assembly qualified name
                stream.Position = stream.Position - 1;
                var length = stream.Read7BitEncodedInt();
                length -= 0x80;
                var bytes = new byte[length];
                stream.Read(bytes, 0, length);
                var typeName = Encoding.UTF8.GetString(bytes);
                return Type.GetType(typeName);
            }
        }
    }
}
