namespace __RUNTIME__.Xml
{
    /// <summary>A prefix bound to a namespace.</summary>
    public sealed class XmlNamespaceDeclaration
    {
        public XmlNamespaceDeclaration(string prefix, string @namespace)
        {
            Prefix = prefix;
            Namespace = @namespace;
        }

        /// <summary>The prefix, without the xmlns colon.</summary>
        public string Prefix { get; private set; }

        /// <summary>The namespace it stands for.</summary>
        public string Namespace { get; private set; }
    }
}
