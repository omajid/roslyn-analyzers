﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

#pragma warning disable RS1012 // Start action has no registered actions.

namespace Analyzer.Utilities
{
    internal static class AnalyzerOptionsExtensions
    {
        private static readonly ConditionalWeakTable<AnalyzerOptions, CategorizedAnalyzerConfigOptions> s_cachedOptions
            = new ConditionalWeakTable<AnalyzerOptions, CategorizedAnalyzerConfigOptions>();

        public static SymbolVisibilityGroup GetSymbolVisibilityGroupOption(
            this AnalyzerOptions options,
            DiagnosticDescriptor rule,
            SymbolVisibilityGroup defaultValue,
            CancellationToken cancellationToken)
            => options.GetEnumOptionValue(EditorConfigOptionNames.ApiSurface, rule, defaultValue, cancellationToken);

        public static TEnum GetEnumOptionValue<TEnum>(
            this AnalyzerOptions options,
            string optionName,
            DiagnosticDescriptor rule,
            TEnum defaultValue,
            CancellationToken cancellationToken)
            where TEnum: struct
        {
            var analyzerConfigOptions = options.GetOrComputeCategorizedAnalyzerConfigOptions(cancellationToken);
            return analyzerConfigOptions.GetEnumOptionValue(optionName, rule, defaultValue);
        }

        public static bool GetBoolOptionValue(
            this AnalyzerOptions options,
            string optionName,
            DiagnosticDescriptor rule,
            bool defaultValue,
            CancellationToken cancellationToken)
        {
            var analyzerConfigOptions = options.GetOrComputeCategorizedAnalyzerConfigOptions(cancellationToken);
            return analyzerConfigOptions.GetOptionValue(optionName, rule, bool.TryParse, defaultValue);
        }

        private static CategorizedAnalyzerConfigOptions GetOrComputeCategorizedAnalyzerConfigOptions(
            this AnalyzerOptions options, CancellationToken cancellationToken)
        {
            // TryGetValue upfront to avoid allocating createValueCallback if the entry already exists. 
            if (s_cachedOptions.TryGetValue(options, out var categorizedAnalyzerConfigOptions))
            {
                return categorizedAnalyzerConfigOptions;
            }

            var createValueCallback = new ConditionalWeakTable<AnalyzerOptions, CategorizedAnalyzerConfigOptions>.CreateValueCallback(_ => ComputeCategorizedAnalyzerConfigOptions());
            return s_cachedOptions.GetValue(options, createValueCallback);

            // Local functions.
            CategorizedAnalyzerConfigOptions ComputeCategorizedAnalyzerConfigOptions()
            {
                foreach (var additionalFile in options.AdditionalFiles)
                {
                    var fileName = Path.GetFileName(additionalFile.Path);
                    if (fileName.Equals(".editorconfig", StringComparison.OrdinalIgnoreCase))
                    {
                        var text = additionalFile.GetText(cancellationToken);
                        return EditorConfigParser.Parse(text);
                    }
                }

                return CategorizedAnalyzerConfigOptions.Empty;
            }
        }
    }
}
