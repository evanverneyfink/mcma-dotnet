namespace Mcma.Core
{
    public abstract class McmaDynamicResource : McmaDynamicObject, IMcmaResource
    {
        public string Id { get; set; }
    }
}