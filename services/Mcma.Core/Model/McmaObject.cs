namespace Mcma.Core
{
    public abstract class McmaObject : IMcmaObject
    {
        protected McmaObject()
        {
            Type = GetType().Name;
        }

        public string Type { get; set; }
    }
}