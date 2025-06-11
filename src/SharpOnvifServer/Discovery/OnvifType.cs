namespace SharpOnvifServer.Discovery
{
    public class OnvifType
    {
        public string TypeNamespace { get; set; }
        public string TypeName { get; set; }

        public OnvifType()
        { }

        public OnvifType(string typeNamespace, string typeName)
        {
            this.TypeNamespace = typeNamespace;
            this.TypeName = typeName;
        }
    }
}
