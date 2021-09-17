using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;


public class LevelManagerScript : MonoBehaviour
{
    // Fungsi Singleton
    private static LevelManagerScript _instance = null;

    public static LevelManagerScript Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<LevelManagerScript>();
            }
            return _instance;
        }
    }

    [SerializeField] private int _maxLives;
    [SerializeField] private int _totalEnemy;
    [SerializeField] private int _totalEnemyTap;

    [SerializeField] private GameObject _panel;
    [SerializeField] private Text _statusInfo;
    [SerializeField] private Text _livesInfo;
    [SerializeField] private Text _totalEnemyInfo;

    [SerializeField] private Transform _towerUIParent;
    [SerializeField] private GameObject _towerUIPrefab;
    [SerializeField] private GameObject[] _towerPrefabs;
    [SerializeField] private GameObject[] _enemyPrefabs;

    [SerializeField] private Transform[] _enemyPaths;
    [SerializeField] private float _spawnDelay = 5f;

    private List<TowerScript> _spawnedTowers = new List<TowerScript>();
    private List<EnemyScript> _spawnedEnemies = new List<EnemyScript>();
    private List<BulletScript> _spawnedBullets = new List<BulletScript>();
    private List<Vector2> _listParent = new List<Vector2>();

    private int _currentLives;
    private int _enemyCounter;
    private float _runningSpawnDelay;
    private bool _tapMode = false;
    private bool _readyToTap = false;
    private bool _canSpawn = false;

    public bool IsOver { get; private set; }

    private void Start()
    {
        Time.timeScale = 1;
        SetCurrentLives(_maxLives);
        SetTotalEnemy(_totalEnemy);
        _canSpawn = true;

        InstantiateAllTowerUI();
    }


    private void Update()
    {
        // Counter untuk spawn enemy dalam jeda waktu yang ditentukan
        // Time.unscaledDeltaTime adalah deltaTime yang independent, tidak terpengaruh oleh apapun kecuali game object itu sendiri,
        // jadi bisa digunakan sebagai penghitung waktu
        _runningSpawnDelay -= Time.unscaledDeltaTime;
        
        if (_runningSpawnDelay < 0f)
        {
            SpawnEnemy();
            _runningSpawnDelay = _spawnDelay;
        }

        foreach (TowerScript tower in _spawnedTowers)
        {
            tower.CheckNearestEnemy(_spawnedEnemies);
            tower.SeekTarget();
            tower.ShootTarget();
        }

        foreach (EnemyScript enemy in _spawnedEnemies)
        {
            if (!enemy.gameObject.activeSelf) continue;

            // Kenapa nilainya 0.2? Karena untuk lebih mentoleransi perbedaan posisi,
            // akan terlalu sulit jika perbedaan posisinya harus 0 atau sama persis
            if (Vector2.Distance(enemy.transform.position, enemy.TargetPosition) < 0.2f)
            {
                enemy.SetCurrentPathIndex(enemy.CurrentPathIndex + 1);

                if (enemy.CurrentPathIndex < _enemyPaths.Length)
                {
                    enemy.SetTargetPosition(_enemyPaths[enemy.CurrentPathIndex].position);
                }
                else
                {
                    enemy.gameObject.SetActive(false);
                    SetCurrentLives(_currentLives-=1);
                }
            }
            else
            {
                enemy.MoveToTarget();
            }
        }

        if (_readyToTap && _currentLives>0)
        {
            //jika semua enemy dikalahkan
            int result = 0;
            foreach (EnemyScript item in _spawnedEnemies)
            {
                if (!item.gameObject.activeSelf) result++;
            }

            if (result == _spawnedEnemies.Count)
            {
                SetTotalEnemy(_totalEnemyTap);
                _panel.SetActive(true);
                _panel.transform.GetChild(1).gameObject.SetActive(true);
                SetTapMode();
            }
        }

        //Mode Tap-Tap
        if (_tapMode)
        {
            if (Input.GetMouseButtonDown(0))
            {
                //Start Tap-Tap
                if (!_canSpawn)
                {
                    _panel.transform.GetChild(1).gameObject.SetActive(false);
                    _panel.SetActive(false);
                    _spawnDelay = 1f;
                    Time.timeScale = 2f;
                    _canSpawn = true;
                }

                //Konversi mouse posisi screen to wotld
                Vector3 tap = Input.mousePosition;
                tap.z = Camera.main.transform.position.z;
                tap = Camera.main.ScreenToWorldPoint(tap);

                foreach (EnemyScript enemy in _spawnedEnemies)
                {
                    //Jika Klick Mouse Mengenai Enemy, Enemy Dibuang 
                    if (Vector2.Distance(enemy.transform.position, tap) < 0.4f && enemy.gameObject.activeSelf)
                    {
                        AudioScript.Instance.PlaySFX("hit-enemy");
                        enemy.gameObject.SetActive(false);
                    }
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        if (IsOver)
        {
            //jika semua enemy dikalahkan
            int result = 0;
            foreach (EnemyScript item in _spawnedEnemies)
            {
                if (!item.gameObject.activeSelf) result++;
            }

            if (result==_spawnedEnemies.Count)
            {
                _panel.gameObject.SetActive(true);
                _panel.transform.GetChild(0).gameObject.SetActive(true);
                Time.timeScale = 0;
            }
        }
    }

    //Mendaftarkan Tower yang di-spawn agar bisa dikontrol oleh LevelManager
    //Menampilkan seluruh Tower yang tersedia pada UI Tower Selection
    private void InstantiateAllTowerUI()
    {
        foreach(GameObject tower in _towerPrefabs)
        {
            GameObject newTowerUIObj = Instantiate(_towerUIPrefab.gameObject, _towerUIParent);
            TowerUIScript newTowerUI = newTowerUIObj.GetComponent<TowerUIScript>();

            newTowerUI.SetTowerPrefab(tower.GetComponent<TowerScript>());
            newTowerUI.transform.name = tower.name;
        }
    }

    public void RegisterSpawnedTower(TowerScript tower, Vector2 posParent)
    {
        //Jika Parent Sudah Terdaftar Replace
        if (_listParent.Contains(posParent))
        {
            Destroy(_spawnedTowers[_listParent.IndexOf(posParent)].gameObject);
            _spawnedTowers.RemoveAt(_listParent.IndexOf(posParent));
            _listParent.Remove(posParent);
        }

        _listParent.Add(posParent);
        _spawnedTowers.Add(tower);
    }

    private void SpawnEnemy()
    {
        if (_canSpawn)
        {
            SetTotalEnemy(--_enemyCounter);

            if (_enemyCounter < 0) return;

            int randomIndex = Random.Range(0, _enemyPrefabs.Length);
            string enemyIndexString = (randomIndex + 1).ToString();

            GameObject newEnemyObj = _spawnedEnemies.Find(
                e => !e.gameObject.activeSelf && e.name.Contains(enemyIndexString)
            )?.gameObject;

            if (newEnemyObj == null)
            {
                newEnemyObj = Instantiate(_enemyPrefabs[randomIndex].gameObject);
            }

            EnemyScript newEnemy = newEnemyObj.GetComponent<EnemyScript>();

            if (!_spawnedEnemies.Contains(newEnemy))
            {
                _spawnedEnemies.Add(newEnemy);
            }

            newEnemy.transform.position = _enemyPaths[0].position;
            newEnemy.SetTargetPosition(_enemyPaths[1].position);
            newEnemy.SetLast(_enemyCounter == 0);
            newEnemy.gameObject.SetActive(true);
        }
    }

    public BulletScript GetBulletFromPool(BulletScript prefab)
    {
        GameObject newBulletObj = _spawnedBullets.Find(
            b => !b.gameObject.activeSelf && b.name.Contains(prefab.name)
        )?.gameObject;

        if (newBulletObj == null)
        {
            newBulletObj = Instantiate(prefab.gameObject);
        }

        BulletScript newBullet = newBulletObj.GetComponent<BulletScript>();
        if (!_spawnedBullets.Contains(newBullet))
        {
            _spawnedBullets.Add(newBullet);
        }

        return newBullet;
    }

    public void ExplodeAt(Vector2 point, float radius, int damage)
    {
        foreach (EnemyScript enemy in _spawnedEnemies)
        {
            if (enemy.gameObject.activeSelf)
            {
                if (Vector2.Distance(enemy.transform.position, point) <= radius)
                {
                    enemy.ReduceEnemyHealth(damage);
                }
            }
        }
    }

    public void SetCurrentLives(int currentLives)
    {
        _currentLives = currentLives;
        _livesInfo.text = $"Lives: {_currentLives}";

        if (_currentLives <= 0)
        {
            _statusInfo.text = "You Lose!";
            _statusInfo.color = Color.red;

            _panel.gameObject.SetActive(true);
            _panel.transform.GetChild(0).gameObject.SetActive(true);
            Time.timeScale = 0;
        }
    }

    public void SetTotalEnemy(int totalEnemy)
    {
        _enemyCounter = totalEnemy;
        _totalEnemyInfo.text = $"Total Enemy: {Mathf.Max(_enemyCounter, 0)}";
    }

    //Enemy Terakhir DiKalahkan
    public void LastEnemyKilled()
    {
        _canSpawn = false;
        if (!_tapMode)
        {
            _readyToTap = true;
        }
        else SetGameOver(true);
    }

    public void SetTapMode()
    {
        _readyToTap = false;
        _tapMode = true;
    }

    public void SetGameOver(bool isWin)
    {
        IsOver = true;
        _statusInfo.text = isWin ? "You Win!" : "You Lose!";
        _statusInfo.color = isWin ? Color.yellow : Color.red;
    }

    // Untuk menampilkan garis penghubung dalam window Scene
    // tanpa harus di-Play terlebih dahulu
    private void OnDrawGizmos()
    {
        for (int i = 0; i < _enemyPaths.Length - 1; i++)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(_enemyPaths[i].position, _enemyPaths[i + 1].position);
        }
    }

    public void Exit()
    {
        Application.Quit();
    }
}