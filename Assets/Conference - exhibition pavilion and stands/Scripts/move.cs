using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class move : MonoBehaviour
{
    public float _speed = .2f;
    public bool _xAxis, _yAxis, _zAxis;
    string _moveThem ="0";
    void Start()
    {
        //Checkers();
    }

    void Update()
    {
        Checkers();

        switch (_moveThem)
        {
            case "0":
                break;
            case "x":
                transform.localPosition = new Vector3(transform.position.x + _speed * Time.deltaTime, transform.position.y, transform.position.z);
                break;
            case "y":
                transform.localPosition = new Vector3(transform.position.x, transform.position.y + _speed * Time.deltaTime, transform.position.z);
                break;
            case "z":
                transform.localPosition = new Vector3(transform.position.x, transform.position.y, transform.position.z + _speed * Time.deltaTime);
                break;
            case "xz":
                transform.localPosition = new Vector3(transform.position.x + _speed * Time.deltaTime, transform.position.y, transform.position.z + _speed * Time.deltaTime);
                break;
            case "yz":
                transform.localPosition = new Vector3(transform.position.x, transform.position.y + _speed * Time.deltaTime, transform.position.z + _speed * Time.deltaTime);
                break;
            case "xy":
                transform.localPosition = new Vector3(transform.position.x + _speed * Time.deltaTime, transform.position.y + _speed * Time.deltaTime, transform.position.z);
                break;
            case "xyz":
                transform.localPosition = new Vector3(transform.position.x + _speed * Time.deltaTime, transform.position.y + _speed * Time.deltaTime, transform.position.z + _speed * Time.deltaTime);
                break;
            default:
                break;
        }

        void Checkers()
        {
            if (_xAxis == true)// 1-x-x
            {
                if (_yAxis == true)// 1-1-x
                {
                    if (_zAxis == true)// 1-1-1
                    {
                        _moveThem = "xyz";
                    }
                    else// 1-1-0
                    {
                        _moveThem = "xy";
                    }
                }
                else if (_zAxis == true)// 1-0-1
                {
                    _moveThem = "xz";
                }
                else// 1-0-0
                {
                    _moveThem = "x";
                }

            }
            else if (_yAxis == true) // 0-1-x
            {
                if (_zAxis == true)// 0-1-1
                {
                    _moveThem = "yz";
                }
                else// 0-1-0
                {
                    _moveThem = "y";
                }
            }
            else // 0-0-x
            {
                if (_zAxis == true)// 0-0-1
                {
                    _moveThem = "z";
                }
                else// 0-0-0
                {
                    _moveThem = "0";
                }
            }
        }
    }
}
