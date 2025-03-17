using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MyTools
{
    public enum EventID
    {
        #region Utilities
        UTILS_ROTATE_ARSPACE,
        #endregion

        FINISH_MARKER_TRACKING,

        #region User Study
        USER_STUDY_START,
        USER_STUDY_RESET_CONDITION,
        USER_STUDY_RESET_ROUND,
        USER_STUDY_ACTIVATE_SPHERE,
        USER_STUDY_CORRECT_FEEDBACK,
        USER_STUDY_CHANGE_RING_NUMBER,
        USER_STUDY_CHANGE_SPHERE_NUMBER_PER_RING,

        #region ZED Client
        ZED_CLIENT_FPV_DISPLAY,
        #endregion





        #endregion
    }


}