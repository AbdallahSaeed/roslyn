﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace Roslyn.Utilities
{
    internal static class GeneratedCodeUtilities
    {
        private static readonly string[] s_autoGeneratedStrings = new[] { "<autogenerated", "<auto-generated" };

        internal static bool IsGeneratedSymbolWithGeneratedCodeAttribute(
            ISymbol symbol, INamedTypeSymbol generatedCodeAttribute)
        {
            Debug.Assert(symbol != null);
            Debug.Assert(generatedCodeAttribute != null);

            // Don't check this for namespaces.  Namespaces cannot have attributes on them. And, 
            // currently, calling DeclaringSyntaxReferences on an INamespaceSymbol is more expensive
            // than is desirable.
            if (symbol.Kind != SymbolKind.Namespace)
            {
                // GeneratedCodeAttribute can only be applied once on a symbol.
                // For partial symbols with more than one definition, we must treat them as non-generated code symbols.
                if (symbol.DeclaringSyntaxReferences.Length > 1)
                {
                    return false;
                }

                foreach (var attribute in symbol.GetAttributes())
                {
                    if (generatedCodeAttribute.Equals(attribute.AttributeClass))
                    {
                        return true;
                    }
                }
            }

            return symbol.ContainingSymbol != null && IsGeneratedSymbolWithGeneratedCodeAttribute(symbol.ContainingSymbol, generatedCodeAttribute);
        }

        internal static bool IsGeneratedCode(
            SyntaxTree tree, Func<SyntaxTrivia, bool> isComment, CancellationToken cancellationToken)
        {
            return IsGeneratedCodeFile(tree.FilePath) ||
                   BeginsWithAutoGeneratedComment(tree, isComment, cancellationToken);
        }

        private static bool IsGeneratedCodeFile(string filePath)
        {
            if (!string.IsNullOrEmpty(filePath))
            {
                var fileName = PathUtilities.GetFileName(filePath);
                if (fileName.StartsWith("TemporaryGeneratedFile_", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                var extension = PathUtilities.GetExtension(fileName);
                if (!string.IsNullOrEmpty(extension))
                {
                    var fileNameWithoutExtension = PathUtilities.GetFileName(filePath, includeExtension: false);
                    if (fileNameWithoutExtension.EndsWith(".designer", StringComparison.OrdinalIgnoreCase) ||
                        fileNameWithoutExtension.EndsWith(".generated", StringComparison.OrdinalIgnoreCase) ||
                        fileNameWithoutExtension.EndsWith(".g", StringComparison.OrdinalIgnoreCase) ||
                        fileNameWithoutExtension.EndsWith(".g.i", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool BeginsWithAutoGeneratedComment(
            SyntaxTree tree, Func<SyntaxTrivia, bool> isComment, CancellationToken cancellationToken)
        {
            var root = tree.GetRoot(cancellationToken);
            if (root.HasLeadingTrivia)
            {
                var leadingTrivia = root.GetLeadingTrivia();

                foreach (var trivia in leadingTrivia)
                {
                    if (!isComment(trivia))
                    {
                        continue;
                    }

                    var text = trivia.ToString();

                    // Check to see if the text of the comment contains an auto generated comment.
                    foreach (var autoGenerated in s_autoGeneratedStrings)
                    {
                        if (text.Contains(autoGenerated))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}