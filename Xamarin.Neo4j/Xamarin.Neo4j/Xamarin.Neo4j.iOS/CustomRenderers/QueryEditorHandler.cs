//
// QueryEditorHandler.cs
//
// Trevi Awater
// 13-01-2022
//
// © Xamarin.Neo4j.iOS
//

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using CoreGraphics;
using Foundation;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using UIKit;
using Xamarin.Neo4j.Controls;

namespace Xamarin.Neo4j.iOS.CustomRenderers
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

        protected override void ConnectHandler(MauiTextView platformView)
        {
            base.ConnectHandler(platformView);

            platformView.AutocorrectionType = UITextAutocorrectionType.No;
            platformView.AutocapitalizationType = UITextAutocapitalizationType.None;
            platformView.SpellCheckingType = UITextSpellCheckingType.No;
            platformView.KeyboardType = UIKeyboardType.Default;

            const float accessoryHeight = 58f;
            const float buttonWidth = 33f;
            const float executeButtonWidth = 90f;
            const float padding = 8f;
            const float pillHeight = 38f;

            var keys = new[]
            {
                ("(", "("), (")", ")"), ("[", "["), ("]", "]"),
                ("{", "{"), ("}", "}"), (":", ":"), ("-", "-"),
                ("\u2192", "->"), ("\u2190", "<-"), ("\"", "\""),
                ("'", "'"), (".", "."), ("=", "="), ("*", "*"), ("$", "$")
            };

            // Outer accessory — clear so system background shows through
            var accessoryView = new UIView(new CGRect(0, 0, 0, accessoryHeight));
            accessoryView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth;
            accessoryView.BackgroundColor = UIColor.Clear;

            // Pill container for scroll view
            var pillContainer = new UIView();
            pillContainer.TranslatesAutoresizingMaskIntoConstraints = false;
            pillContainer.BackgroundColor = UIColor.SystemBackground.ColorWithAlpha(0.9f);
            pillContainer.Layer.CornerRadius = pillHeight / 2f;
            pillContainer.Layer.MasksToBounds = true;

            var scrollView = new UIScrollView();
            scrollView.TranslatesAutoresizingMaskIntoConstraints = false;
            scrollView.ShowsHorizontalScrollIndicator = false;
            scrollView.ShowsVerticalScrollIndicator = false;
            scrollView.Bounces = false;
            scrollView.BackgroundColor = UIColor.Clear;

            var xOffset = 0f;
            foreach (var (label, insertion) in keys)
            {
                var captured = insertion;
                var btn = new UIButton(UIButtonType.System);
                btn.SetTitle(label, UIControlState.Normal);
                btn.Frame = new CGRect(xOffset, 0, buttonWidth, pillHeight);
                btn.TouchUpInside += (s, e) => platformView.InsertText(captured);
                scrollView.AddSubview(btn);
                xOffset += buttonWidth;
            }

            scrollView.ContentSize = new CGSize(xOffset, pillHeight);
            pillContainer.AddSubview(scrollView);

            NSLayoutConstraint.ActivateConstraints(new[]
            {
                scrollView.LeadingAnchor.ConstraintEqualTo(pillContainer.LeadingAnchor),
                scrollView.TrailingAnchor.ConstraintEqualTo(pillContainer.TrailingAnchor),
                scrollView.TopAnchor.ConstraintEqualTo(pillContainer.TopAnchor),
                scrollView.BottomAnchor.ConstraintEqualTo(pillContainer.BottomAnchor),
            });

            accessoryView.AddSubview(pillContainer);

            NSLayoutConstraint.ActivateConstraints(new[]
            {
                pillContainer.LeadingAnchor.ConstraintEqualTo(accessoryView.LeadingAnchor, padding),
                pillContainer.CenterYAnchor.ConstraintEqualTo(accessoryView.CenterYAnchor),
                pillContainer.HeightAnchor.ConstraintEqualTo(pillHeight),
            });

            platformView.InputAccessoryView = accessoryView;

            // Ensure accessory hides when keyboard is dismissed by external gesture/scroll
            var keyboardHideObserver = UIKeyboard.Notifications.ObserveWillHide((s, args) =>
            {
                // The InputAccessoryView is removed with the keyboard automatically.
                // But if the editor remains first responder after an external dismiss
                // (e.g. interactive dismiss on scroll), resign to fully hide the bar.
                if (platformView.IsFirstResponder)
                    platformView.ResignFirstResponder();
            });

            platformView.Changed += (s, e) =>
            {
                AutoCapitalizeKeywords(platformView, _keyWords);
                HighlightWords(platformView, _keyWords);
            };

            HighlightWords(platformView, _keyWords);
        }

        private static void HighlightWords(UITextView platformView, IEnumerable<string> wordsToHighlight)
        {
            var text = PreprocessText(platformView.Text ?? string.Empty);
            var attributedText = new NSMutableAttributedString(text);
            attributedText.AddAttribute(UIStringAttributeKey.ForegroundColor,
                UIColor.Label,
                new NSRange(0, text.Length));

            foreach (var word in wordsToHighlight)
            {
                var regex = new Regex("\\b" + Regex.Escape(word) + "\\b", RegexOptions.IgnoreCase);

                foreach (Match match in regex.Matches(text))
                {
                    attributedText.AddAttribute(UIStringAttributeKey.ForegroundColor,
                        UIColor.FromRGBA(137 / 255f, 152 / 255f, 46 / 255f, 1f),
                        new NSRange(match.Index, match.Length));
                }
            }

            ApplyQuoteTextColorFormatting(text, attributedText, "'(.*?)'",
                UIColor.FromRGBA(174 / 255f, 139 / 255f, 45 / 255f, 1f));

            ApplyQuoteTextColorFormatting(text, attributedText, "\"(.*?)\"",
                UIColor.FromRGBA(174 / 255f, 139 / 255f, 45 / 255f, 1f));

            var cursorPosition = platformView.SelectedRange;
            platformView.AttributedText = attributedText;
            platformView.SelectedRange = cursorPosition;
        }

        private static void ApplyQuoteTextColorFormatting(string text, NSMutableAttributedString attributedText,
            string quotePattern, UIColor color)
        {
            var quoteRegex = new Regex(quotePattern);

            foreach (Match match in quoteRegex.Matches(text))
            {
                attributedText.AddAttribute(UIStringAttributeKey.ForegroundColor, color,
                    new NSRange(match.Index, match.Length));
            }
        }

        private static void AutoCapitalizeKeywords(UITextView platformView, IEnumerable<string> keywords)
        {
            if (!Microsoft.Maui.Storage.Preferences.Default.Get("auto_capitalize", true)) return;

            var text = platformView.Text ?? string.Empty;
            var cursorPos = platformView.SelectedRange;
            var changed = false;
            var chars = text.ToCharArray();

            foreach (var word in keywords)
            {
                var regex = new Regex("\\b" + Regex.Escape(word) + "\\b", RegexOptions.IgnoreCase);
                foreach (Match match in regex.Matches(text))
                {
                    var segment = text.Substring(match.Index, match.Length);
                    if (segment == word) continue;
                    if ((nint)cursorPos.Location == match.Index + match.Length) continue;

                    for (var i = 0; i < match.Length; i++)
                        chars[match.Index + i] = word[i];
                    changed = true;
                }
            }

            if (changed)
            {
                platformView.Text = new string(chars);
                platformView.SelectedRange = cursorPos;
            }
        }

        private static string PreprocessText(string text)
        {
            text = text.Replace("\u2018", "'");
            text = text.Replace("\u2019", "'");
            text = text.Replace("\u201c", "\"");
            text = text.Replace("\u201d", "\"");
            return text;
        }
    }
}
