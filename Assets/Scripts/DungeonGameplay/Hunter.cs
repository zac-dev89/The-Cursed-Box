using UnityEngine;
using Mirror;

public class Hunter : NetworkBehaviour
{
    public Camera hunterCam;
    
    public void ShouldShowHunterCam(bool show)
    {
        hunterCam.gameObject.SetActive(show);
    }

    public RenderTexture GetHunterFootageRef()
    {
        return hunterCam.targetTexture;
    }
}
