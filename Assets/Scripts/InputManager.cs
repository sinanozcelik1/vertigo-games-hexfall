using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputManager : SuperClass
{
    private bool validTouch;
    private GridManager GridManagerObject;
    private Vector2 touchStartPosition;
    private Hexagon selectedHexagon;

    void Start()
    {
        GridManagerObject = GridManager.instance;
    }

    void Update()
    {
        if (GridManagerObject.InputAvailabile() && Input.touchCount > ZERO)
        {
            Vector3 wp = Camera.main.ScreenToWorldPoint(Input.GetTouch(ZERO).position);
            Vector2 touchPos = new Vector2(wp.x, wp.y);
            Collider2D collider = Physics2D.OverlapPoint(touchPos);
            selectedHexagon = GridManagerObject.GetSelectedHexagon();

            TouchDetection();
            CheckSelection(collider);
            CheckRotation();
        }
    }

    private void TouchDetection()
    {
        if (Input.GetTouch(ZERO).phase == TouchPhase.Began)
        {
            validTouch = true;
            touchStartPosition = Input.GetTouch(ZERO).position;
        }
    }

    private void CheckSelection(Collider2D collider)
    {
        if (collider != null && collider.transform.tag == TAG_HEXAGON)
        {
            if (Input.GetTouch(ZERO).phase == TouchPhase.Ended && validTouch)
            {
                validTouch = false;
                GridManagerObject.Select(collider);
            }
        }
    }

    private void CheckRotation()
    {
        if (Input.GetTouch(ZERO).phase == TouchPhase.Moved && validTouch)
        {
            Vector2 touchCurrentPosition = Input.GetTouch(ZERO).position;
            float distanceX = touchCurrentPosition.x - touchStartPosition.x;
            float distanceY = touchCurrentPosition.y - touchStartPosition.y;

            if ((Mathf.Abs(distanceX) > HEX_ROTATE_SLIDE_DISTANCE || Mathf.Abs(distanceY) > HEX_ROTATE_SLIDE_DISTANCE) && selectedHexagon != null)
            {
                Vector3 screenPosition = Camera.main.WorldToScreenPoint(selectedHexagon.transform.position);

                bool triggerOnX = Mathf.Abs(distanceX) > Mathf.Abs(distanceY);
                bool swipeRightUp = triggerOnX ? distanceX > ZERO : distanceY > ZERO;
                bool touchThanHex = triggerOnX ? touchCurrentPosition.y > screenPosition.y : touchCurrentPosition.x > screenPosition.x;
                bool clockWise = triggerOnX ? swipeRightUp == touchThanHex : swipeRightUp != touchThanHex;

                validTouch = false;
                GridManagerObject.Rotate(clockWise);
            }
        }
    }
}
