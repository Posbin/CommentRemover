using System.Linq;

namespace CommentRemover
{
    class Program
    {
        static void Main(string[] args)
        {
            bool removingXmlTags = false;
            var pathes = args.ToList();
            if (args[0] == "-x")
            {
                removingXmlTags = true;
                pathes.RemoveAt(0);
            }

            var cr = new CommentRemover(removingXmlTags);
            foreach (var path in pathes)
            {
                cr.Remove(path);
            }
        }
    }
}
