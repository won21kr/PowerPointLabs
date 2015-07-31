﻿using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Office.Core;
using PowerPointLabs.ImageSearch.Util;
using PowerPointLabs.Utils;
using Shape = Microsoft.Office.Interop.PowerPoint.Shape;
using Shapes = Microsoft.Office.Interop.PowerPoint.Shapes;

namespace PowerPointLabs.ImageSearch.Handler.Effect
{
    public class TextBoxes
    {
        public const int Margin = 25;

        private List<Shape> TextShapes { get; set; }

        private readonly float _slideWidth;

        private readonly float _slideHeight;

        private Position _pos;

        private Alignment _align;

        private float _left;

        private float _top;

        # region APIs
        public TextBoxes(Shapes shapes, float slideWidth, float slideHeight)
        {
            _slideWidth = slideWidth;
            _slideHeight = slideHeight;
            TextShapes = new List<Shape>();
            foreach (Shape shape in shapes)
            {
                if ((shape.Type != MsoShapeType.msoPlaceholder
                        && shape.Type != MsoShapeType.msoTextBox)
                        || shape.TextFrame.HasText == MsoTriState.msoFalse
                        || StringUtil.IsEmpty(shape.TextFrame2.TextRange.Paragraphs.TrimText().Text))
                {
                    continue;
                }
                TextShapes.Add(shape);
            }
        }

        public TextBoxes SetPosition(Position pos)
        {
            _pos = pos;
            return this;
        }

        public TextBoxes SetAlignment(Alignment align)
        {
            _align = align;
            return this;
        }

        public void StartBoxing()
        {
            if (_pos != Position.Original)
            {
                StartPositioning();
            }
            else
            {
                RecoverPositioning();
            }
        }

        public TextBoxInfo GetTextBoxesInfo()
        {
            return GetTextBoxesInfo(TextShapes);
        }

        public static void AddMargin(TextBoxInfo textboxesInfo)
        {
            textboxesInfo.Left -= Margin;
            textboxesInfo.Top -= Margin;
            textboxesInfo.Width += 2 * Margin;
            textboxesInfo.Height += 2 * Margin;
        }

        # endregion

        # region Helper Funcs
        private void RecoverPositioning()
        {
            foreach (var textShape in TextShapes)
            {
                if (StringUtil.IsNotEmpty(textShape.Tags[Tag.OriginalShapeLeft]))
                {
                    textShape.Left = float.Parse(textShape.Tags[Tag.OriginalShapeLeft]);
                    textShape.Tags.Add(Tag.OriginalShapeLeft, "");
                }
                if (StringUtil.IsNotEmpty(textShape.Tags[Tag.OriginalShapeTop]))
                {
                    textShape.Top = float.Parse(textShape.Tags[Tag.OriginalShapeTop]);
                    textShape.Tags.Add(Tag.OriginalShapeTop, "");
                }
                if (StringUtil.IsNotEmpty(textShape.Tags[Tag.OriginalTextAlignment]))
                {
                    textShape.TextEffect.Alignment =
                        StringUtil.GetTextEffectAlignment(textShape.Tags[Tag.OriginalTextAlignment]);
                    textShape.Tags.Add(Tag.OriginalTextAlignment, "");
                }
            }
        }

        private void StartPositioning()
        {
            // decide which textbox is on top of the other
            SortTextBoxes();

            SetupTextBoxesAlignment();

            var boxesInfo = GetTextBoxesInfo(TextShapes);
            SetupTextBoxesPosition(boxesInfo);

            var accumulatedHeight = 0f;
            foreach (var textShape in TextShapes)
            {
                var singleBoxInfo = GetTextBoxInfo(textShape);

                AdjustShapeLeft(textShape, boxesInfo, singleBoxInfo);
                AdjustShapeTop(textShape, singleBoxInfo, accumulatedHeight);
                accumulatedHeight += singleBoxInfo.Height;
            }
        }

        private void AdjustShapeTop(Shape textShape, TextBoxInfo singleBoxInfo, float accumulatedHeight)
        {
            ShapeUtil.AddTag(textShape, Tag.OriginalShapeTop, textShape.Top.ToString(CultureInfo.InvariantCulture));
            textShape.Top = _top + textShape.Top - (singleBoxInfo.Top - accumulatedHeight);
        }

        private void AdjustShapeLeft(Shape textShape, TextBoxInfo boxesInfo, TextBoxInfo singleBoxInfo)
        {
            ShapeUtil.AddTag(textShape, Tag.OriginalShapeLeft, textShape.Left.ToString(CultureInfo.InvariantCulture));
            switch (_align)
            {
                case Alignment.Left:
                    textShape.Left = _left + textShape.Left - (singleBoxInfo.Left - 0f);
                    break;
                case Alignment.Centre:
                    textShape.Left = _left + textShape.Left -
                                     (singleBoxInfo.Left - (boxesInfo.Width/2 - singleBoxInfo.Width/2));
                    break;
                case Alignment.Right:
                    textShape.Left = _left + textShape.Left - (singleBoxInfo.Left - (boxesInfo.Width - singleBoxInfo.Width));
                    break;
            }
        }

        private void SetupTextBoxesAlignment()
        {
            HandleAutoAlignment();
            switch (_align)
            {
                case Alignment.Left:
                    SetTextAlignment(MsoTextEffectAlignment.msoTextEffectAlignmentLeft);
                    break;
                case Alignment.Centre:
                    SetTextAlignment(MsoTextEffectAlignment.msoTextEffectAlignmentCentered);
                    break;
                case Alignment.Right:
                    SetTextAlignment(MsoTextEffectAlignment.msoTextEffectAlignmentRight);
                    break;
            }
        }

        private void HandleAutoAlignment()
        {
            if (_align != Alignment.Auto) return;
            switch (_pos)
            {
                case Position.TopLeft:
                case Position.Left:
                case Position.BottomLeft:
                    _align = Alignment.Left;
                    break;
                case Position.Top:
                case Position.Centre:
                case Position.Bottom:
                    _align = Alignment.Centre;
                    break;
                case Position.TopRight:
                case Position.Right:
                case Position.BottomRight:
                    _align = Alignment.Right;
                    break;
            }
        }

        private void SetTextAlignment(MsoTextEffectAlignment alignment)
        {
            foreach (var shape in TextShapes)
            {
                ShapeUtil.AddTag(shape, Tag.OriginalTextAlignment, 
                    StringUtil.GetTextEffectAlignment(shape.TextEffect.Alignment));
                shape.TextEffect.Alignment = alignment;
            }
        }

        private void SetupTextBoxesPosition(TextBoxInfo boxesInfo)
        {
            switch (_pos)
            {
                case Position.TopLeft:
                case Position.Left:
                case Position.BottomLeft:
                    _left = Margin;
                    break;
                case Position.Top:
                case Position.Centre:
                case Position.Bottom:
                    _left = _slideWidth/2 - boxesInfo.Width/2;
                    break;
                case Position.TopRight:
                case Position.Right:
                case Position.BottomRight:
                    _left = _slideWidth - boxesInfo.Width - Margin;
                    break;
            }
            switch (_pos)
            {
                case Position.TopLeft:
                case Position.Top:
                case Position.TopRight:
                    _top = Margin;
                    break;
                case Position.Left:
                case Position.Centre:
                case Position.Right:
                    _top = _slideHeight/2 - boxesInfo.Height/2;
                    break;
                case Position.BottomLeft:
                case Position.Bottom:
                case Position.BottomRight:
                    _top = _slideHeight - boxesInfo.Height - Margin;
                    break;
            }
        }

        private TextBoxInfo GetTextBoxInfo(Shape textShape)
        {
            var result = new TextBoxInfo();
            var paragraphs = textShape.TextFrame2.TextRange.Paragraphs;
            foreach (TextRange2 textRange in paragraphs)
            {
                var paragraph = textRange.TrimText();
                if (StringUtil.IsNotEmpty(paragraph.Text))
                {
                    result.Left = paragraph.BoundLeft < result.Left ? paragraph.BoundLeft : result.Left;
                    result.Top = paragraph.BoundTop < result.Top ? paragraph.BoundTop : result.Top;
                    result.Width = paragraph.BoundWidth > result.Width ? paragraph.BoundWidth : result.Width;
                }
            }
            result.Height = paragraphs.BoundHeight;
            return result;
        }

        private TextBoxInfo GetTextBoxesInfo(IEnumerable<Shape> textShapes)
        {
            var result = new TextBoxInfo();
            foreach (var partialResult in textShapes.Select(GetTextBoxInfo))
            {
                result.Left = partialResult.Left < result.Left ? partialResult.Left : result.Left;
                result.Top = partialResult.Top < result.Top ? partialResult.Top : result.Top;
                result.Width = partialResult.Width > result.Width ? partialResult.Width : result.Width;
                result.Height += partialResult.Height;
            }
            return result;
        }

        // rule:
        // top > left > name: Title > name: Subtitle > name: Text > other
        // 
        // exp sorted result:
        // most top textbox at the first element,
        // most bottom textbox at the last element
        private void SortTextBoxes()
        {
            TextShapes.Sort((shape1, shape2) =>
            {
                if ((int)(shape2.Top - shape1.Top) != 0)
                {
                    return (int) (shape1.Top - shape2.Top);
                }
                if ((int)(shape2.Left - shape1.Left) != 0)
                {
                    return (int) (shape1.Left - shape2.Left);
                }
                if (shape1.Name.StartsWith("Title"))
                {
                    return -1;
                }
                if (shape2.Name.StartsWith("Title"))
                {
                    return 1;
                }
                if (shape1.Name.StartsWith("Subtitle"))
                {
                    return -1;
                }
                if (shape2.Name.StartsWith("Subtitle"))
                {
                    return 1;
                }
                if (shape1.Name.StartsWith("Text"))
                {
                    return -1;
                }
                if (shape2.Name.StartsWith("Text"))
                {
                    return 1;
                }
                return -1;
            });
        }

        # endregion
    }
}
