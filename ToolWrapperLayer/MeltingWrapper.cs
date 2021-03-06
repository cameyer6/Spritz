﻿using System.Collections.Generic;
using System.IO;

namespace ToolWrapperLayer
{
    /// <summary>
    /// Computes the melting temperature of nucleic acid duplexes, given salt concentrations and such.
    /// </summary>
    public class MeltingWrapper :
        IInstallable
    {
        #region Installation Methods

        /// <summary>
        /// Writes an installation script for the program MELTING.
        /// </summary>
        /// <param name="spritzDirectory"></param>
        /// <returns></returns>
        public string WriteInstallScript(string spritzDirectory)
        {
            string scriptPath = WrapperUtility.GetInstallationScriptPath(spritzDirectory, "InstallMelting.bash");
            WrapperUtility.GenerateScript(scriptPath, new List<string>
            {
                WrapperUtility.ChangeToToolsDirectoryCommand(spritzDirectory),
                "if [ ! -d MELTING5.1.1 ]; then",
                "  wget --no-check -O MELTING5.1.1.tar.gz http://sourceforge.net/projects/melting/files/meltingJava/melting5/MELTING5.1.1.tar.gz/download",
                "  tar -xvf MELTING5.1.1.tar.gz; rm MELTING5.1.1.tar.gz",
                "  cd MELTING5.1.1",
                "  ./Install.unices",
                "fi"
            });
            return scriptPath;
        }

        /// <summary>
        /// Writes a script for removing cufflinks.
        /// </summary>
        /// <param name="spritzDirectory"></param>
        /// <returns></returns>
        public string WriteRemoveScript(string spritzDirectory)
        {
            return null;
        }

        #endregion Installation Methods
    }
}