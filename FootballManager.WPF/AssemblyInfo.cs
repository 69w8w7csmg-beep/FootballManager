// מטרה: קובע היכן WPF מחפש משאבי ערכת נושא.
// מי משתמש: מנגנון המשאבים של WPF.
// פעולות: מאפיין ThemeInfo.
// תלויות: System.Windows.
using System.Windows;

[assembly: ThemeInfo(
    ResourceDictionaryLocation.None,            // אין מילון ערכת נושא ספציפי לכל עמוד.
                                                // משאבים חסרים יחפשו במילון הכללי של היישום.
    ResourceDictionaryLocation.SourceAssembly   // המילון הכללי נמצא בהרכבת היישום.
)]
