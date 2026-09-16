using ForbiddenTechnologyPack.Core;
using ForbiddenTechnologyPack.Game.Save;
using PeterHan.PLib.UI;
using UnityEngine;

namespace ForbiddenTechnologyPack.Game.Safety {
    public static class SafeRemovalDialog {
        public static void ShowFirstConfirmation(object ignored) {
            if (global::Game.Instance == null || ForbiddenTechSaveData.Instance == null) {
                ShowMessage(STRINGS.UI.SAFEREMOVAL.TITLE, STRINGS.UI.SAFEREMOVAL.NO_ACTIVE_GAME);
                return;
            }

            ShowConfirmation("ForbiddenTechSafeRemovalFirst",
                STRINGS.UI.SAFEREMOVAL.FIRST_WARNING,
                STRINGS.UI.SAFEREMOVAL.CONTINUE,
                delegate { ShowSecondConfirmation(); });
        }

        private static void ShowSecondConfirmation() {
            ShowConfirmation("ForbiddenTechSafeRemovalSecond",
                STRINGS.UI.SAFEREMOVAL.SECOND_WARNING,
                STRINGS.UI.SAFEREMOVAL.CONVERT_AND_STOP,
                delegate {
                    var report = SafeRemovalController.Execute();
                    ShowReport(report);
                });
        }

        private static void ShowConfirmation(string name, string message, string confirmText,
                System.Action onConfirm) {
            var dialog = new PDialog(name) {
                Title = STRINGS.UI.SAFEREMOVAL.TITLE,
                SortKey = 250f,
                Size = new Vector2(560f, 0f)
            };
            dialog.Body.AddChild(new PLabel(name + "Body") {
                Text = message,
                Margin = new RectOffset(12, 12, 12, 12)
            });
            dialog.AddButton("confirm", confirmText, null, PUITuning.Colors.ButtonBlueStyle);
            dialog.AddButton("cancel", STRINGS.UI.SAFEREMOVAL.CANCEL, null,
                PUITuning.Colors.ButtonPinkStyle);
            dialog.DialogClosed = delegate(string option) {
                if (option == "confirm" && onConfirm != null) {
                    onConfirm();
                }
            };
            dialog.Show();
        }

        private static void ShowReport(SafeRemovalReport report) {
            var message = report.IsComplete
                ? string.Format(STRINGS.UI.SAFEREMOVAL.SUCCESS,
                    report.ConvertedObjectCount, report.ConvertedMassKg,
                    report.RemovedBuildingCount, report.ReturnedInputCount)
                : string.Format(STRINGS.UI.SAFEREMOVAL.INCOMPLETE,
                    report.RemainingCustomObjectCount);
            ShowMessage(STRINGS.UI.SAFEREMOVAL.TITLE, message);
        }

        private static void ShowMessage(string title, string message) {
            var dialog = new PDialog("ForbiddenTechSafeRemovalMessage") {
                Title = title,
                SortKey = 250f,
                Size = new Vector2(560f, 0f)
            };
            dialog.Body.AddChild(new PLabel("Message") {
                Text = message,
                Margin = new RectOffset(12, 12, 12, 12)
            });
            dialog.AddButton("close", STRINGS.UI.SAFEREMOVAL.CLOSE);
            dialog.Show();
        }
    }
}
