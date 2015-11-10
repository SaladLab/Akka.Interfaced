using System;
using System.Linq;
using System.Reflection;
using Akka.Interfaced;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Akka.Interfaced.LogFilter
{
    // This is quite simple class for dealing with serializing ISurrogated instances.
    // Without this converter, json default serailizer easily gets lost in inspecting IActorRef object.
    // Because sole purpose is just writing log, it uses ToString instead of ISurrogated context.
    internal class SurrogateSimpleConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            if (typeof(Akka.Util.ISurrogated).IsAssignableFrom(objectType))
                return true;

            return false;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue(value.ToString());
        }
    }
}
