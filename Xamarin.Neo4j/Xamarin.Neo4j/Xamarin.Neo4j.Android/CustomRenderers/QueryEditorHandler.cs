//
// QueryEditorHandler.cs
//
// © Xamarin.Neo4j.Android
//

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Android.Content;
using Android.Graphics;
using Android.Text;
using Android.Text.Style;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using Xamarin.Neo4j.Controls;

namespace Xamarin.Neo4j.Android.CustomRenderers
{
    public class QueryEditorHandler : EditorHandler
    {
        private readonly string[] _keyWords =
        [
            // Clauses
            "CALL", "CREATE", "DELETE", "DETACH", "FOREACH", "LOAD", "MATCH", "MERGE", "OPTIONAL", "REMOVE", "RETURN", "SET", "START", "UNION", "UNWIND", "WITH",

            // Subclauses
            "LIMIT", "ORDER", "SKIP", "WHERE", "YIELD",

            // Modifiers
            "ASC", "ASCENDING", "ASSERT", "BY", "CSV", "DESC", "DESCENDING", "ON",

            // Expressions
            "ALL", "CASE", "COUNT", "ELSE", "END", "EXISTS", "THEN", "WHEN",

            // Operators
            "AND", "AS", "CONTAINS", "DISTINCT", "ENDS", "IN", "IS", "NOT", "OR", "STARTS", "XOR",

            // Schema
            "CONSTRAINT", "CREATE", "DROP", "EXISTS", "INDEX", "NODE", "KEY", "UNIQUE",

            // Hints
            "INDEX", "JOIN", "SCAN", "USING",

            // Literals
            "FALSE", "NULL", "TRUE"
        ];

        protected override void ConnectHandler(MauiAppCompatEditText platformView)
        {
            base.ConnectHandler(platformView);

            // Disable autocorrect, autocapitalize, and spellcheck
            platformView.InputType = InputTypes.ClassText
                | InputTypes.TextFlagMultiLine
                | InputTypes.TextFlagNoSuggestions;

            // Keyboard toolbar with symbol keys and Execute button
            var toolbar = BuildToolbar(platformView);
            platformView.FocusChange += (s, e) =>
                toolbar.Visibility = e.HasFocus ? ViewStates.Visible : ViewStates.Gone;

            // Syntax highlighting
            platformView.AddTextChangedListener(new CypherTextWatcher(platformView, _keyWords));
            HighlightSyntax(platformView, _keyWords);
        }

        private LinearLayout BuildToolbar(MauiAppCompatEditText platformView)
        {
            var context = platformView.Context;

            var toolbar = new LinearLayout(context)
            {
                Orientation = Orientation.Horizontal,
                LayoutParameters = new LinearLayout.LayoutParams(
                    ViewGroup.LayoutParams.MatchParent,
                    ViewGroup.LayoutParams.WrapContent)
            };
            toolbar.SetBackgroundColor(Color.ParseColor("#F2F2F7"));
            toolbar.SetPadding(8, 8, 8, 8);
            toolbar.Visibility = ViewStates.Gone;

            // Scrollable symbol key strip
            var scrollView = new HorizontalScrollView(context)
            {
                LayoutParameters = new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 1f)
            };
            scrollView.HorizontalScrollBarEnabled = false;

            var keyRow = new LinearLayout(context) { Orientation = Orientation.Horizontal };
            foreach (var key in new[] { "(", ")", "[", "]", ":", "-", "->", "<-" })
            {
                var captured = key;
                var btn = CreatePillButton(context, captured);
                btn.Click += (s, e) =>
                {
                    var start = Math.Max(platformView.SelectionStart, 0);
                    var end = Math.Max(platformView.SelectionEnd, 0);
                    platformView.EditableText?.Replace(Math.Min(start, end), Math.Max(start, end), captured);
                };
                keyRow.AddView(btn);
            }

            scrollView.AddView(keyRow);
            toolbar.AddView(scrollView);

            // Execute button
            var executeBtn = CreatePillButton(context, "Execute");
            executeBtn.SetTextColor(Color.White);
            executeBtn.SetBackgroundColor(Color.ParseColor("#007AFF"));
            executeBtn.Click += (s, e) =>
            {
                if (VirtualView is QueryEditor queryEditor)
                {
                    queryEditor.RaiseExecuteClicked();
                    var imm = (InputMethodManager)context.GetSystemService(Context.InputMethodService);
                    imm?.HideSoftInputFromWindow(platformView.WindowToken, 0);
                }
            };
            toolbar.AddView(executeBtn);

            // Attach toolbar to the parent view hierarchy
            platformView.ViewTreeObserver.GlobalLayout += (s, e) =>
            {
                if (platformView.Parent is ViewGroup parent && toolbar.Parent == null)
                    parent.AddView(toolbar);
            };

            return toolbar;
        }

        private static Button CreatePillButton(Context context, string label)
        {
            return new Button(context)
            {
                Text = label,
                LayoutParameters = new LinearLayout.LayoutParams(
                    ViewGroup.LayoutParams.WrapContent,
                    ViewGroup.LayoutParams.WrapContent)
                {
                    MarginStart = 4,
                    MarginEnd = 4
                }
            };
        }

        // ── Syntax highlighting ───────────────────────────────────────────────

        internal static void HighlightSyntax(MauiAppCompatEditText platformView, IEnumerable<string> keywords)
        {
            var text = PreprocessText(platformView.Text ?? string.Empty);
            var spannable = new SpannableStringBuilder(text);

            var keywordColor = Color.ParseColor("#899832");
            var literalColor = Color.ParseColor("#AE8B2D");

            foreach (var word in keywords)
            {
                var regex = new Regex("\\b" + Regex.Escape(word) + "\\b", RegexOptions.IgnoreCase);
                foreach (Match match in regex.Matches(text))
                    spannable.SetSpan(new ForegroundColorSpan(keywordColor),
                        match.Index, match.Index + match.Length,
                        SpanTypes.ExclusiveExclusive);
            }

            ApplyQuoteHighlight(text, spannable, "'(.*?)'", literalColor);
            ApplyQuoteHighlight(text, spannable, "\"(.*?)\"", literalColor);

            var selStart = platformView.SelectionStart;
            var selEnd = platformView.SelectionEnd;
            platformView.SetText(spannable, TextView.BufferType.Spannable);
            if (selStart >= 0 && selEnd <= spannable.Length())
                platformView.SetSelection(selStart, selEnd);
        }

        private static void ApplyQuoteHighlight(string text, SpannableStringBuilder spannable,
            string pattern, Color color)
        {
            foreach (Match match in new Regex(pattern).Matches(text))
                spannable.SetSpan(new ForegroundColorSpan(color),
                    match.Index, match.Index + match.Length,
                    SpanTypes.ExclusiveExclusive);
        }

        private static string PreprocessText(string text) =>
            text.Replace("\u2018", "'").Replace("\u2019", "'")
                .Replace("\u201c", "\"").Replace("\u201d", "\"");

        // ── Inner helpers ─────────────────────────────────────────────────────

        private sealed class CypherTextWatcher : Java.Lang.Object, ITextWatcher
        {
            private readonly MauiAppCompatEditText _view;
            private readonly string[] _keywords;
            private bool _updating;

            public CypherTextWatcher(MauiAppCompatEditText view, string[] keywords)
            {
                _view = view;
                _keywords = keywords;
            }

            public void BeforeTextChanged(Java.Lang.ICharSequence s, int start, int count, int after) { }
            public void OnTextChanged(Java.Lang.ICharSequence s, int start, int before, int count) { }

            public void AfterTextChanged(IEditable s)
            {
                if (_updating) return;
                _updating = true;
                try { HighlightSyntax(_view, _keywords); }
                finally { _updating = false; }
            }
        }
    }
}
