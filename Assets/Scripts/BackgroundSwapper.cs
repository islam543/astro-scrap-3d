using UnityEngine;
using UnityEngine.UI; // **Crucial!** This namespace allows you to use UI elements like 'Image'

public class BackgroundSwapper : MonoBehaviour
{
    [Header("Background Elements")]
    // Refernce to the Panel GameObject's Image component in the Hierarchy
    [Tooltip("Drag the Panel object from Hierarchy here")]
    public Image backgroundPanel;

    // References to your image assets in the Project window
    [Tooltip("Drag the original background sprite from Project folder here")]
    public Sprite mainBackgroundSprite;

    [Tooltip("Drag the settings background sprite from Project folder here")]
    public Sprite settingsBackgroundSprite;


    // This public method will be linked to the Settings Button
    public void ChangeToSettingsBackground()
    {
        // First check that we have assigned the assets to avoid errors
        if (backgroundPanel != null && settingsBackgroundSprite != null)
        {
            // Update the Panel's sprite to be the new image
            backgroundPanel.sprite = settingsBackgroundSprite;
        }
    }

    // A separate method to go back (link this to your standard Back button)
    public void ResetToMainBackground()
    {
        if (backgroundPanel != null && mainBackgroundSprite != null)
        {
            // Update the Panel's sprite back to original
            backgroundPanel.sprite = mainBackgroundSprite;
        }
    }
}