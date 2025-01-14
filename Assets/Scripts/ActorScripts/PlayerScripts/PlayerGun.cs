﻿using UnityEngine;

public class PlayerGun : MonoBehaviour
{
    public GameObject casingPrefab;
    public Transform barrelLocation;
    public Transform casingExitLocation;
    [SerializeField] private Animator _animator = default;
    [SerializeField] private GameObject muzzleFlashPrefab = default;
    [SerializeField] private GameObject _impactEffect = default;
    [SerializeField] private Camera _camera = default;
    [SerializeField] private PlayerUI _playerUI = default;
    [SerializeField] private EntityAudio _playerGunAudio = default;
    [SerializeField] private GunSO _gunSO = default;
    private readonly float _fireRate = 0.6f;
    private readonly int _fireRange = 100;
    private float _currentFireRate = 0.6f;
    private bool _canShoot = true;


    void Start()
    {
        if (barrelLocation == null)
            barrelLocation = transform;
    }

    void Update()
    {
        if (!_canShoot)
        {
            _currentFireRate -= Time.deltaTime;
            if (_currentFireRate <= 0.0f)
            {
                _canShoot = true;
                _currentFireRate = _fireRate;
            }
        }
    }

    public void Shoot()
    {
        if (_canShoot)
        {
            if (_gunSO.currentClipSize > 0)
            {
                _canShoot = false;
                _animator.SetTrigger("Fire");
                _playerGunAudio.Play("Shot");
                Physics.Raycast(_camera.transform.position, _camera.transform.forward, out RaycastHit hit, _fireRange);
                if (hit.collider != null)
                {
                    _playerGunAudio.Play("BulletConcrete");
                    if (hit.collider.gameObject.TryGetComponent(out IDamageable damageable))
                    {
                        if (damageable.GetHealth() > 1)
                        {
                            _playerUI.PlayerReticleUI.ShowHitCrosshair();
                        }
                        else
                        {
                            _playerUI.PlayerReticleUI.ShowKillCrosshair();
                        }
                        damageable.TakeDamage(1);
                    }
                    GameObject bulletHole = Instantiate(_impactEffect, hit.point, Quaternion.FromToRotation(Vector3.up, hit.normal));
                    bulletHole.transform.SetParent(hit.transform);
                }
                Instantiate(muzzleFlashPrefab, barrelLocation.position, barrelLocation.rotation);
                _gunSO.currentClipSize--;
                _playerUI.PlayerGunUI.SetCurrentClip(_gunSO.currentClipSize);
            }
            else
            {
                Reload();
            }
        }
    }

    public void CasingRelease()
    {
        GameObject casing = Instantiate(casingPrefab, casingExitLocation.position, casingExitLocation.rotation);
        casing.GetComponent<Rigidbody>().AddExplosionForce(550f, (casingExitLocation.position - casingExitLocation.right * 0.3f - casingExitLocation.up * 0.6f), 1f);
        casing.GetComponent<Rigidbody>().AddTorque(new Vector3(0, Random.Range(100f, 500f), Random.Range(10f, 1000f)), ForceMode.Impulse);
    }

    public void Reload()
    {
        if (_gunSO.totalAmmoAmount != 0)
        {
            _playerGunAudio.Play("Reload");
            int clipToLoad = _gunSO.maxClipSize - _gunSO.currentClipSize;
            if (_gunSO.totalAmmoAmount < clipToLoad)
            {
                clipToLoad = _gunSO.totalAmmoAmount;
            }
            _gunSO.currentClipSize += clipToLoad;
            _gunSO.totalAmmoAmount -= clipToLoad;
            _playerUI.PlayerGunUI.SetCurrentClip(_gunSO.currentClipSize);
            _playerUI.PlayerGunUI.SetTotalAmmo(_gunSO.totalAmmoAmount);
        }
    }

    public GunSO GetGunData()
    {
        return _gunSO;
    }
}