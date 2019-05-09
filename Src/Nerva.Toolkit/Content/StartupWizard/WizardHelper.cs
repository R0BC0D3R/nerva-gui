using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using AngryWasp.Helpers;
using Eto.Drawing;
using Eto.Forms;
using Nerva.Toolkit.Helpers;

namespace Nerva.Toolkit.Content.Wizard
{
    public abstract class WizardContent
    {
        public abstract string Title { get; }
        public WizardDialog Parent { get; set; }
        public abstract Control Content { get; }

        public abstract Control CreateContent();
        public virtual void OnAssignContent() { }
        public virtual void OnNext() { }
        public virtual void OnBack() { }
    }

    public class SetupWizard
    {
        private static bool isRunning = false;
        public static bool IsRunning => isRunning;

        private WizardDialog dlg;

        public SetupWizard()
        {
            isRunning = true;

            WizardContent[] pages = new WizardContent[]
            {
                new IntroContent(),
                new GetCliContent(),
                new WalletSetupContent(),
                new FinishedContent()
            };

            dlg = new WizardDialog(pages);
            dlg.AllowNavigation(true);
        }

        public void Run()
        {
            dlg.ShowModal();
            isRunning = false;
        }
    }
}