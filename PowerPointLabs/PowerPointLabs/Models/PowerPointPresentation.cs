﻿using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Office.Core;
using Microsoft.Office.Interop.PowerPoint;

namespace PowerPointLabs.Models
{
    internal class PowerPointPresentation
    {
        # region Properties
        private string _name;

        public string FullName
        {
            get
            {
                return Path + @"\" + Name;
            }
        }

        public string FullNameNoExtension
        {
            get
            {
                return Path + @"\" + NameNoExtension;
            }
        }

        public string Name
        {
            get { return _name; }
            set
            {
                NameNoExtension = value;
                _name = value + ".pptx";
            }
        }

        public string NameNoExtension { get; private set; }

        public bool Opened
        {
            get
            {
                return
                    Globals.ThisAddIn.Application.Presentations.Cast<Presentation>().Any(
                        presentation => presentation.Name == Name);
            }
        }

        public string Path { get; set; }

        public Presentation Presentation { get; set; }

        public bool Saved
        {
            get { return Presentation.Saved == MsoTriState.msoTrue; }
        }

        public List<PowerPointSlide> Slides
        {
            get
            {
                var slides = new List<PowerPointSlide>();

                var interopSlides = Presentation.Slides;

                foreach (Slide interopSlide in interopSlides)
                {
                    var s = PowerPointSlide.FromSlideFactory(interopSlide);
                    slides.Add(s);
                }

                return slides;
            }
        }

        public List<PowerPointSlide> SelectedSlides
        {
            get
            {
                var interopSlides = Presentation.Application.ActiveWindow.Selection.SlideRange;
                var slides = new List<PowerPointSlide>();

                foreach (Slide interopSlide in interopSlides)
                {
                    PowerPointSlide s = PowerPointSlide.FromSlideFactory(interopSlide);
                    slides.Add(s);
                }

                return slides;
            }
        }

        public int SlideCount
        {
            get { return Presentation.Slides.Count; }
        }

        public float SlideWidth
        {
            get
            {
                var dimensions = Presentation.PageSetup;
                return dimensions.SlideWidth;
            }
        }

        public float SlideHeight
        {
            get
            {
                var dimensions = Presentation.PageSetup;
                return dimensions.SlideHeight;
            }
        }
        # endregion

        # region Constructors
        public PowerPointPresentation()
        {
            Presentation = null;
        }

        public PowerPointPresentation(string path, string name)
        {
            Path = path;
            Name = name;
        }

        public PowerPointPresentation(Presentation presentation)
        {
            Presentation = presentation;
        }
        # endregion

        # region API
        public PowerPointSlide AddSlide(PpSlideLayout layout = PpSlideLayout.ppLayoutText, string name = "")
        {
            if (!Opened)
            {
                return null;
            }

            var customLayout = Presentation.SlideMaster.CustomLayouts[layout];
            var newSlide = Presentation.Slides.AddSlide(SlideCount + 1, customLayout);

            if (name != "")
            {
                newSlide.Name = name;
            }

            var slideFromFactory = PowerPointSlide.FromSlideFactory(newSlide);

            Slides.Add(slideFromFactory);

            return slideFromFactory;
        }

        // TODO: need to be verified
        public void RemoveSlide(string name)
        {
            var slides = Presentation.Slides;

            foreach (Slide slide in slides)
            {
                if (slide.Name == name)
                {
                    slide.Delete();
                    break;
                }
            }
        }

        public void RemoveSlide(int index)
        {
            // here we need to change the 0-based index to 1-based index!!!
            Presentation.Slides[index + 1].Delete();
        }

        public bool Create(bool withWidow, bool focus)
        {
            if (File.Exists(FullName))
            {
                return false;
            }

            var workingWindow = Globals.ThisAddIn.Application.ActiveWindow;

            Presentation = Globals.ThisAddIn.Application.Presentations.Add(BoolToMsoTriState(withWidow));
            Presentation.SaveAs(FullNameNoExtension);

            if (!focus)
            {
                workingWindow.Activate();
            }

            return true;
        }

        public virtual void Close()
        {
            Presentation.Close();
            Presentation = null;

            Trace.TraceInformation("Presentation " + NameNoExtension + " is closed.");
        }

        public virtual bool Open(bool readOnly = false, bool untitled = false, bool withWindow = true, bool focus = true)
        {
            if (Opened)
            {
                return false;
            }

            // if the file doesn't exist, create and open the file then return
            if (Create(withWindow, focus))
            {
                return true;
            }

            var workingWindow = Globals.ThisAddIn.Application.ActiveWindow;

            try
            {
                Presentation = Globals.ThisAddIn.Application.Presentations.Open(FullName, BoolToMsoTriState(readOnly),
                                                                                BoolToMsoTriState(untitled),
                                                                                BoolToMsoTriState(withWindow));
            }
            catch (System.Exception)
            {
                return false;
            }

            if (!focus)
            {
                workingWindow.Activate();
            }

            return true;
        }

        public void Save()
        {
            if (Presentation != null)
            {
                Presentation.Save();
            }
        }
        # endregion

        # region Helper Functions
        private MsoTriState BoolToMsoTriState(bool value)
        {
            return value ? MsoTriState.msoTrue : MsoTriState.msoFalse;
        }
        # endregion
    }
}
