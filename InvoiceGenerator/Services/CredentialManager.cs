using System;
using System.Runtime.InteropServices;
using System.Text;

namespace InvoiceGenerator.Services
{
    public class CredentialManager
    {
        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool CredWrite(ref CREDENTIAL credential, uint flags);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool CredRead(string target, CRED_TYPE type, uint flags, out IntPtr credential);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool CredFree(IntPtr credential);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool CredDelete(string target, CRED_TYPE type, uint flags);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct CREDENTIAL
        {
            public uint Flags;
            public CRED_TYPE Type;
            public string TargetName;
            public string Comment;
            public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
            public uint CredentialBlobSize;
            public IntPtr CredentialBlob;
            public uint Persist;
            public uint AttributeCount;
            public IntPtr Attributes;
            public string TargetAlias;
            public string UserName;
        }

        private enum CRED_TYPE : uint
        {
            Generic = 1,
            DomainPassword = 2,
            DomainCertificate = 3,
            DomainVisiblePassword = 4,
            GenericCertificate = 5,
            DomainExtended = 6,
            Maximum = 7,
            MaximumEx = Maximum + 1000,
        }

        private const uint CRED_PRESERVE_CREDENTIAL_BLOB = 0x1;
        private const uint CRED_FLAGS_PROMPT_NOW = 0x2;
        private const uint CREDUI_FLAGS_ALWAYS_SHOW_UI = 0x80;

        /// <summary>
        /// Saves a credential (password) to Windows Credential Manager
        /// </summary>
        public static void SavePassword(string targetName, string userName, string password)
        {
            try
            {
                byte[] passwordBytes = Encoding.Unicode.GetBytes(password);

                CREDENTIAL cred = new CREDENTIAL()
                {
                    Flags = 0,
                    Type = CRED_TYPE.Generic,
                    TargetName = targetName,
                    CredentialBlobSize = (uint)passwordBytes.Length,
                    CredentialBlob = Marshal.AllocCoTaskMem(passwordBytes.Length),
                    Persist = 2, // CRED_PERSIST_LOCAL_MACHINE
                    UserName = userName
                };

                try
                {
                    Marshal.Copy(passwordBytes, 0, cred.CredentialBlob, passwordBytes.Length);

                    if (!CredWrite(ref cred, 0))
                    {
                        throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(),
                            "Failed to save credential to Windows Credential Manager");
                    }
                }
                finally
                {
                    if (cred.CredentialBlob != IntPtr.Zero)
                        Marshal.FreeCoTaskMem(cred.CredentialBlob);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error saving password to credential manager: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Retrieves a credential (password) from Windows Credential Manager
        /// </summary>
        public static string? GetPassword(string targetName)
        {
            try
            {
                IntPtr credentialPtr;

                if (!CredRead(targetName, CRED_TYPE.Generic, 0, out credentialPtr))
                {
                    return null; // Credential not found
                }

                try
                {
                    CREDENTIAL cred = Marshal.PtrToStructure<CREDENTIAL>(credentialPtr);
                    string password = Marshal.PtrToStringUni(cred.CredentialBlob, (int)cred.CredentialBlobSize / 2);
                    return password;
                }
                finally
                {
                    CredFree(credentialPtr);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving password from credential manager: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Deletes a credential from Windows Credential Manager
        /// </summary>
        public static void DeletePassword(string targetName)
        {
            try
            {
                if (!CredDelete(targetName, CRED_TYPE.Generic, 0))
                {
                    throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(),
                        "Failed to delete credential from Windows Credential Manager");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error deleting password from credential manager: {ex.Message}", ex);
            }
        }
    }
}
