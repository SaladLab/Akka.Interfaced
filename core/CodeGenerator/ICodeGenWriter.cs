namespace CodeGen
{
    public interface ICodeGenWriter
    {
        void AddUsing(string @namespace);
        void AddCode(string code);
        void PushNamespace(string @namespace);
        void PopNamespace();
        void PushRegion(string @region);
        void PopRegion();
    }
}
