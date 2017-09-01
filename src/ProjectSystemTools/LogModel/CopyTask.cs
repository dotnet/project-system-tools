﻿using System.Collections.Generic;
using System.Linq;

namespace Microsoft.VisualStudio.ProjectSystem.Tools.LogModel
{
    internal sealed class CopyTask : Task
    {
        private IEnumerable<FileCopyOperation> _fileCopyOperations;

        public IEnumerable<FileCopyOperation> FileCopyOperations => _fileCopyOperations ?? (_fileCopyOperations = GetFileCopyOperations());

        private IEnumerable<FileCopyOperation> GetFileCopyOperations()
        {
            if (!HasChildren)
            {
                return Enumerable.Empty<FileCopyOperation>();
            }

            List<FileCopyOperation> list = new List<FileCopyOperation>();

            foreach (var message in this.Children.OfType<Message>())
            {
                var text = message.Text;
                if (text.StartsWith(copyingFileFrom))
                {
                    var operation = ParseCopyingFileFrom(text, copyingFileFrom, to);
                    list.Add(operation);
                }
                else if (text.StartsWith(creatingHardLink))
                {
                    var operation = ParseCopyingFileFrom(text, creatingHardLink, to);
                    list.Add(operation);
                }
                else if (text.StartsWith(didNotCopy))
                {
                    var operation = ParseCopyingFileFrom(text, didNotCopy, toFile);
                    operation.Copied = false;
                    list.Add(operation);
                }
            }

            return list;
        }

        private static readonly string copyingFileFrom = "Copying file from \"";
        private static readonly string creatingHardLink = "Creating hard link to copy \"";
        private static readonly string didNotCopy = "Did not copy from file \"";
        private static readonly string to = "\" to \"";
        private static readonly string toFile = "\" to file \"";

        private static FileCopyOperation ParseCopyingFileFrom(string text, string prefix, string infix)
        {
            var result = new FileCopyOperation();

            var split = text.IndexOf(infix);
            var prefixLength = prefix.Length;
            int toLength = infix.Length;
            result.Source = text.Substring(prefixLength, split - prefixLength);
            result.Destination = text.Substring(split + toLength, text.Length - 2 - split - toLength);
            result.Copied = true;

            return result;
        }
    }
}
