using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Eto.Drawing;
using Eto.Forms;
using Nerva.Toolkit.Helpers;

namespace Nerva.Toolkit.Content.Wizard
{
    public class WizardDialog : Dialog
    {
        protected Button btnCancel = new Button { Text = "Cancel" };
        protected Button btnNext = new Button { Text = "Next" };
        protected Button btnBack = new Button { Text = "Back" };

        public bool WizardEnd { get; set; } = false;

        private int currentPage = 0;

        private WizardContent[] pages;

        public WizardDialog(WizardContent[] pages)
        {
            this.Resizable = true;
            var scr = Screen.PrimaryScreen;
            Location = new Point((int)(scr.WorkingArea.Width - Size.Width) / 2, (int)(scr.WorkingArea.Height - Size.Height) / 2);

            this.AbortButton = btnCancel;
            this.DefaultButton = btnNext;

            btnBack.Click += (s, e) => OnBack();
            btnNext.Click += (s, e) => OnNext();
            btnCancel.Click += (s, e) => OnCancel();

            this.pages = pages;

            foreach (var p in pages)
                p.Parent = this;
            
            SetButtonsEnabled();
            ConstructContent();
            this.Invalidate(true);
        }

        public void ConstructContent()
        {
            Title = pages[currentPage].Title;

            Content = new StackLayout
            {
                Orientation = Orientation.Vertical,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch,
                Items =
                {
                    new StackLayoutItem(new StackLayout
                    {
                        Orientation = Orientation.Horizontal,
                        HorizontalContentAlignment = HorizontalAlignment.Stretch,
                        VerticalContentAlignment = VerticalAlignment.Stretch,
                        Padding = 10,
                        Spacing = 10,
                        Items = 
                        {
                            new StackLayoutItem(new ImageView
                            {
                                Image = Bitmap.FromResource("Nerva.Toolkit.NERVA-Logo.png", Assembly.GetExecutingAssembly())
                            }, false),
                            new StackLayoutItem(pages[currentPage].Content, true),
                        }
                    }, true),
                    new StackLayoutItem(new StackLayout
                    {
                        Orientation = Orientation.Horizontal,
                        HorizontalContentAlignment = HorizontalAlignment.Right,
                        VerticalContentAlignment = VerticalAlignment.Center,
                        Padding = 10,
                        Spacing = 10,
                        Items =
                        {
                            new StackLayoutItem(null, true),
                            btnCancel,
                            btnBack,
                            btnNext
                        }
                    })
                }
            };

            OnAssignContent();
        }

        protected virtual void OnAssignContent()
        {
            pages[currentPage].OnAssignContent();
            this.ClientSize = Content.Size;
            
        }

        protected override void OnShown(EventArgs e)
        {
            
        }

        protected override void OnPreLoad(EventArgs e)
        {
            
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (!WizardEnd)
            {
                e.Cancel = true;
                OnCancel();
            }
        }

        protected virtual void OnCancel()
        {
            if (MessageBox.Show(Application.Instance.MainForm, "The NERVA Toolkit cannot start until the startup wizard is complete.\r\nAre you sure you wait to quit.", "Wizard Incomplete",
                MessageBoxButtons.YesNo, MessageBoxType.Warning, MessageBoxDefaultButton.No) == DialogResult.Yes)
                Environment.Exit(0);
        }

        protected virtual void OnBack()
        {
            pages[currentPage].OnBack();

            --currentPage;
            if (currentPage < 0)
                currentPage = 0;

            SetButtonsEnabled();
            ConstructContent();
        }

        protected virtual void OnNext()
        {
            pages[currentPage].OnNext();

            ++currentPage;
            if (currentPage >= pages.Length - 1)
                currentPage = pages.Length - 1;

            SetButtonsEnabled();
            ConstructContent();
        }

        private void SetButtonsEnabled()
        {
            Application.Instance.AsyncInvoke(() =>
            {
                btnBack.Enabled = (currentPage > 0);
                btnNext.Enabled = (currentPage < pages.Length - 1);

                if (currentPage >= pages.Length - 1)
                    btnNext.Text = "Finish";
                else
                    btnNext.Text = "Next";
			});
        }

        public void EnableNextButton(bool enable)
        {
            Application.Instance.AsyncInvoke(() =>
            {
                btnNext.Enabled = enable;
			}); 
        }

        public void EnableBackButton(bool enable)
        {
            Application.Instance.AsyncInvoke(() =>
            {
                btnBack.Enabled = enable;
			}); 
        }   

        public void AllowNavigation(bool allow)
        {
            Application.Instance.AsyncInvoke(() =>
            {
                btnBack.Enabled = allow;
                btnNext.Enabled = allow;
			});
        }
    }
}