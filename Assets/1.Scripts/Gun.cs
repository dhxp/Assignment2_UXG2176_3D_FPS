
using UnityEngine;
using TMPro;

public class Gun : MonoBehaviour
{
    //Gun stats
    public int damage;
    public float timeBetweenShooting, spread, range, reloadTime, timeBetweenShots;
    public int magazineSize, bulletsPerTap;
    public bool isAutomaticWeapon;
    int bulletsLeft, bulletsShot;

    //some bools
    bool shooting, readyToShoot, reloading;

    public Camera fpsCam;
    public GameObject muzzleFlash;
    public Transform attackPoint;
    public RaycastHit rayHit;
    public LayerMask whatIsEnemy;

    //Show bullet amount
    public CamShake camShake;
    public float camShakeMagnitude, camShakeDuration;
    public TextMeshProUGUI text;

    public bool allowInvoke = true;

    private void Start()
    {
        bulletsLeft = magazineSize;
        readyToShoot = true;
    }
    void Update()
    {
        MyInput();

        //Set Text
        text.SetText(bulletsLeft + " / " + magazineSize);
    }
    private void MyInput()
    {
        //Input
        if (isAutomaticWeapon) shooting = Input.GetKey(KeyCode.Mouse0);
        else shooting = Input.GetKeyDown(KeyCode.Mouse0);

        if (Input.GetKeyDown(KeyCode.R) && bulletsLeft < magazineSize && !reloading) Reload();

        //Shoot
        if (readyToShoot && shooting && !reloading && bulletsLeft > 0){
            bulletsShot = bulletsPerTap;
            Shoot(); 
        }
    }
    private void Shoot()
    {
        readyToShoot = false;

        //Spread
        float x = Random.Range(-spread, spread);
        float y = Random.Range(-spread, spread);

        //Calc Direction with Spread
        Vector3 direction = Vector3.zero;
        direction = fpsCam.transform.forward + new Vector3(x, y, 0);

        //RayCast
        if (Physics.Raycast(fpsCam.transform.position, direction, out rayHit, range, whatIsEnemy)){
            Debug.Log(rayHit.collider.gameObject.name);

            if(rayHit.collider.CompareTag("Enemy"))
            rayHit.collider.gameObject.SetActive(false);
        }

        Instantiate(muzzleFlash, attackPoint.position, Quaternion.identity);

        //Shake Camera
        camShake.StartCoroutine(camShake.Shake(camShakeDuration, camShakeMagnitude));

        bulletsLeft--;
        bulletsShot--;

        if (allowInvoke)
        {
            Invoke("ShotReset", timeBetweenShooting);
            allowInvoke = false;
        }

        if (bulletsShot > 0 && bulletsLeft > 0)
            Invoke("Shoot", timeBetweenShots);
    }
    private void ShotReset()
    {
        readyToShoot = true;
        allowInvoke = true;
    }
    private void Reload()
    {
        reloading = true;

        Invoke("DoneReloading", reloadTime);
    }
    private void DoneReloading()
    {
        bulletsLeft = magazineSize;
        reloading = false;
    }
}