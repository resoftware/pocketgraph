//
// QueryEditorHandler.cs
//
// © Xamarin.Neo4j.Android
//

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Android.Content;
using Android.Content.Res;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Text;
using Android.Text.Style;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using AndroidX.Core.View;
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

        private LinearLayout _toolbar;
        private bool _toolbarAttached;

        protected override void ConnectHandler(MauiAppCompatEditText platformView)
        {
            base.ConnectHandler(platformView);

            // Disable autocorrect, autocapitalize, and spellcheck
            platformView.InputType = InputTypes.ClassText
                | InputTypes.TextFlagMultiLine
                | InputTypes.TextFlagNoSuggestions;

            // Build the keyboard toolbar
            _toolbar = BuildToolbar(platformView);

            // Attach toolbar once to the content view (stays in hierarchy, just hidden)
            AttachToolbarOnce(platformView);

            // Detect keyboard via visible frame height difference.
            // Edge-to-edge and varying MAUI soft-input modes make WindowInsets unreliable,
            // so we compare the root view height to the visible display frame.
            platformView.ViewTreeObserver.GlobalLayout += (s, e) =>
            {
                var rootView = platformView.RootView;
                if (rootView == null) return;

                var rect = new global::Android.Graphics.Rect();
                rootView.GetWindowVisibleDisplayFrame(rect);
                var screenHeight = rootView.Height;
                var keyboardHeight = screenHeight - rect.Bottom;

                if (keyboardHeight > screenHeight * 0.15 && platformView.HasFocus)
                {
                    if (_toolbar.LayoutParameters is FrameLayout.LayoutParams lp)
                    {
                        lp.BottomMargin = keyboardHeight;
                        _toolbar.LayoutParameters = lp;
                    }
                    _toolbar.Visibility = ViewStates.Visible;
                }
                else
                {
                    _toolbar.Visibility = ViewStates.Gone;
                }
            };

            // Also hide on focus loss
            platformView.FocusChange += (s, e) =>
            {
                if (!e.HasFocus)
                    _toolbar.Visibility = ViewStates.Gone;
            };

            // Syntax highlighting
            platformView.AddTextChangedListener(new CypherTextWatcher(platformView, _keyWords));
            HighlightSyntax(platformView, _keyWords);
        }

        private void AttachToolbarOnce(MauiAppCompatEditText platformView)
        {
            if (_toolbarAttached) return;

            var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
            var decorView = activity?.Window?.DecorView as ViewGroup;
            var contentView = decorView?.FindViewById<FrameLayout>(global::Android.Resource.Id.Content);
            if (contentView == null) return;

            var layoutParams = new FrameLayout.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.WrapContent,
                GravityFlags.Bottom);
            contentView.AddView(_toolbar, layoutParams);
            _toolbar.Visibility = ViewStates.Gone;
            _toolbarAttached = true;
        }

        private LinearLayout BuildToolbar(MauiAppCompatEditText platformView)
        {
            var context = platformView.Context;
            var density = context.Resources.DisplayMetrics.Density;

            var toolbar = new LinearLayout(context)
            {
                Orientation = global::Android.Widget.Orientation.Horizontal,
                LayoutParameters = new LinearLayout.LayoutParams(
                    ViewGroup.LayoutParams.MatchParent,
                    (int)(48 * density))
            };

            // Theme-aware background
            var isDark = (context.Resources.Configuration.UiMode & UiMode.NightMask) == UiMode.NightYes;
            toolbar.SetBackgroundColor(Color.ParseColor(isDark ? "#141414" : "#F2F2F7"));
            toolbar.SetPadding((int)(6 * density), (int)(6 * density), (int)(6 * density), (int)(6 * density));
            toolbar.SetGravity(GravityFlags.CenterVertical);

            // Pill container for symbol keys
            var pillContainer = new LinearLayout(context)
            {
                Orientation = global::Android.Widget.Orientation.Horizontal,
                LayoutParameters = new LinearLayout.LayoutParams(
                    0, (int)(36 * density), 1f)
            };
            var pillBg = new GradientDrawable();
            pillBg.SetCornerRadius(18 * density);
            pillBg.SetColor(Color.ParseColor(isDark ? "#2a2a2a" : "#E8E8ED").ToArgb());
            pillContainer.Background = pillBg;
            pillContainer.SetGravity(GravityFlags.CenterVertical);

            // Scrollable symbol key strip inside pill
            var scrollView = new HorizontalScrollView(context)
            {
                LayoutParameters = new LinearLayout.LayoutParams(
                    ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent)
            };
            scrollView.HorizontalScrollBarEnabled = false;

            var keyRow = new LinearLayout(context)
            {
                Orientation = global::Android.Widget.Orientation.Horizontal,
                LayoutParameters = new LinearLayout.LayoutParams(
                    ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.MatchParent)
            };
            keyRow.SetGravity(GravityFlags.CenterVertical);

            var symbolTextColor = Color.ParseColor(isDark ? "#e2e2e2" : "#0c0c0c");
            foreach (var key in new[] { "(", ")", "[", "]", "{", "}", ":", "-", "->", "<-", "\"", "'", ".", "=", "*", "$" })
            {
                var captured = key;
                var btn = new TextView(context)
                {
                    Text = captured,
                    Gravity = GravityFlags.Center,
                    Clickable = true,
                    Focusable = true,
                    LayoutParameters = new LinearLayout.LayoutParams(
                        (int)(40 * density), ViewGroup.LayoutParams.MatchParent)
                };
                btn.SetTextColor(symbolTextColor);
                btn.SetBackgroundColor(Color.Transparent);
                btn.SetTextSize(global::Android.Util.ComplexUnitType.Sp, 15);
                btn.SetPadding(0, 0, 0, 0);
                btn.SetIncludeFontPadding(false);
                btn.Click += (s, e) =>
                {
                    var start = Math.Max(platformView.SelectionStart, 0);
                    var end = Math.Max(platformView.SelectionEnd, 0);
                    platformView.EditableText?.Replace(Math.Min(start, end), Math.Max(start, end), captured);
                };
                keyRow.AddView(btn);
            }

            scrollView.AddView(keyRow);
            pillContainer.AddView(scrollView);
            toolbar.AddView(pillContainer);

            // Spacer
            var spacer = new global::Android.Views.View(context)
            {
                LayoutParameters = new LinearLayout.LayoutParams((int)(6 * density), 0)
            };
            toolbar.AddView(spacer);

            // Execute button — blue circle with play icon (▶)
            var executeBtn = new TextView(context)
            {
                Text = "\u25B6",
                LayoutParameters = new LinearLayout.LayoutParams(
                    (int)(36 * density), (int)(36 * density)),
                Gravity = GravityFlags.Center,
                Clickable = true,
                Focusable = true
            };
            executeBtn.SetTextColor(Color.White);
            executeBtn.SetTextSize(global::Android.Util.ComplexUnitType.Sp, 18);
            var executeBg = new GradientDrawable();
            executeBg.SetCornerRadius(18 * density);
            executeBg.SetColor(Color.ParseColor("#007AFF").ToArgb());
            executeBtn.Background = executeBg;
            executeBtn.SetPadding(0, 0, 0, 0);
            executeBtn.SetIncludeFontPadding(false);
            executeBtn.Click += (s, e) =>
            {
                if (VirtualView is QueryEditor queryEditor)
                {
                    queryEditor.RaiseExecuteClicked();
                    var imm = (InputMethodManager)context.GetSystemService(Context.InputMethodService);
                    imm?.HideSoftInputFromWindow(platformView.WindowToken, 0);
                }
            };

            // Disable run button when query is empty
            void updateRunEnabled()
            {
                var hasText = !string.IsNullOrWhiteSpace(platformView.Text);
                executeBtn.Enabled = hasText;
                executeBtn.Alpha = hasText ? 1f : 0.35f;
            }
            updateRunEnabled();
            platformView.AddTextChangedListener(new SimpleTextWatcher(updateRunEnabled));

            toolbar.AddView(executeBtn);

            toolbar.Visibility = ViewStates.Gone;
            return toolbar;
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

        private sealed class SimpleTextWatcher : Java.Lang.Object, ITextWatcher
        {
            private readonly Action _callback;
            public SimpleTextWatcher(Action callback) { _callback = callback; }
            public void BeforeTextChanged(Java.Lang.ICharSequence s, int start, int count, int after) { }
            public void OnTextChanged(Java.Lang.ICharSequence s, int start, int before, int count) { }
            public void AfterTextChanged(IEditable s) { _callback(); }
        }

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
                try
                {
                    AutoCapitalizeKeywords(_view, _keywords);
                    HighlightSyntax(_view, _keywords);
                }
                finally { _updating = false; }
            }
        }

        internal static void AutoCapitalizeKeywords(MauiAppCompatEditText platformView, IEnumerable<string> keywords)
        {
            if (!Microsoft.Maui.Storage.Preferences.Default.Get("auto_capitalize", true)) return;

            var text = platformView.Text ?? string.Empty;
            var selStart = platformView.SelectionStart;
            var selEnd = platformView.SelectionEnd;
            var changed = false;
            var chars = text.ToCharArray();

            foreach (var word in keywords)
            {
                var regex = new Regex("\\b" + Regex.Escape(word) + "\\b", RegexOptions.IgnoreCase);
                foreach (Match match in regex.Matches(text))
                {
                    // Skip if already uppercase
                    var segment = text.Substring(match.Index, match.Length);
                    if (segment == word) continue;

                    // Don't capitalize if cursor is right at the end of this word (user still typing)
                    if (selStart == match.Index + match.Length) continue;

                    for (var i = 0; i < match.Length; i++)
                        chars[match.Index + i] = word[i];
                    changed = true;
                }
            }

            if (changed)
            {
                var newText = new string(chars);
                platformView.SetText(newText, TextView.BufferType.Editable);
                if (selStart >= 0 && selStart <= newText.Length)
                    platformView.SetSelection(Math.Min(selStart, newText.Length), Math.Min(selEnd, newText.Length));
            }
        }
    }
}
