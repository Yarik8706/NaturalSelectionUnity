using UnityEngine;

public class ArtificialIntelligence : MonoBehaviour
{
    private static readonly int[] SkillsTotal = new int[4];

    public GameObject bacteriumPrefab;

    public int foodSkill;
    public int attackSkill;
    public int defSkill;
    public float energy = 10;
    public float age;

    private const int InputsCount = 4;
    private Genome _genome;
    private NeuralNetwork _neuralNetwork;

    private Rigidbody2D _rigidbody2D;

    // Start is called before the first frame update
    private void Start()
    {
        _rigidbody2D = gameObject.GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    private void Update()
    {
        transform.eulerAngles = new Vector3(0f, 0f, Mathf.Atan2(_rigidbody2D.velocity.y, _rigidbody2D.velocity.x) * Mathf.Rad2Deg - 90);
        age += Time.deltaTime;
    }

    private void FixedUpdate()
    {
        var vision = 5f + attackSkill;
        var inputs = new float[InputsCount];
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, vision);

        // количество соседних объектов
        var neighboursCount = new float[4];

        // вектара к центрам масс еды, красного, зеленого и синего
        var vectors = new Vector3[4];
        for (var i = 0; i < 4; i++)
        {
            neighboursCount[i] = 0;
            vectors[i] = new Vector3(0f, 0f, 0f);
        }
        foreach (var localGameObject in colliders)
        {
            if(localGameObject.gameObject == gameObject) continue;
            switch (localGameObject.gameObject.name)
            {
                case "food":
                    neighboursCount[0]++;
                    vectors[0] += localGameObject.gameObject.transform.position - transform.position;
                    break;
                case "bacterium":
                {
                    var ai = localGameObject.gameObject.GetComponent<ArtificialIntelligence>();
                    neighboursCount[1] += ai.attackSkill / 3f;
                    vectors[1] += (localGameObject.gameObject.transform.position - transform.position) * ai.attackSkill;
                    neighboursCount[2] += ai.foodSkill / 3f;
                    vectors[2] += (localGameObject.gameObject.transform.position - transform.position) * ai.foodSkill;
                    neighboursCount[3] += ai.defSkill / 3f;
                    vectors[3] += (localGameObject.gameObject.transform.position - transform.position) * ai.defSkill;
                    break;
                }
            }
        }
        for (int i = 0; i < 4; i++)
        {
            if(neighboursCount[i] > 0)
            {
                vectors[i] /= neighboursCount[i] * vision;
                inputs[i] = vectors[i].magnitude;
            }
            else
            {
                inputs[i] = 0f;
            }
        }

        float[] outputs = _neuralNetwork.FeedForward(inputs);
        Vector2 target = new Vector2(0, 0);
        for (int i = 0; i < 4; i++)
        {
            if (!(neighboursCount[i] > 0)) continue;
            Vector2 dir = new Vector2(vectors[i].x, vectors[i].y);
            dir.Normalize();
            target += dir * outputs[i];
        }
        if(target.magnitude > 1f) target.Normalize();
        Vector2 velocity = _rigidbody2D.velocity;
        velocity += target * (0.25f + attackSkill * 0.05f);
        velocity *= 0.98f;
        _rigidbody2D.velocity = velocity;
        //float antibiotics = 1f;
        // концентрация антибиотиков
        // if(transform.position.x < -39) antibiotics = 4;
        // else if(transform.position.x < -20) antibiotics = 3;
        // else if(transform.position.x < -1) antibiotics = 2;
        // antibiotics = Mathf.Max(1f, antibiotics - defSkill);
        energy -= Time.deltaTime; //* antibiotics * antibiotics;
        if(energy < 0f)
        {
            Kill();
        }
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        if(foodSkill == 0) return;
        if(col.gameObject.name == "food")
        {
            Eat(foodSkill);
            Destroy(col.gameObject);
        }
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if(age < 1f) return;
        if(attackSkill == 0) return;
        if (col.gameObject.name != "bacterium") return;
        var artificialIntelligence = col.gameObject.GetComponent<ArtificialIntelligence>();
        if(artificialIntelligence.age < 1f) return;
        var damage = Mathf.Max(0f, attackSkill - artificialIntelligence.defSkill);
        damage *= 4f;
        damage = Mathf.Min(damage, artificialIntelligence.energy);
        artificialIntelligence.energy -= damage * 1.25f;
        Eat(damage);
        if(artificialIntelligence.energy == 0f) artificialIntelligence.Kill();
    }

    public void Init(Genome newGenome)
    {
        _genome = newGenome;
        var col = new Color(0.1f, 0.1f, 0.25f, 1f);
        var size = 0.75f;
        for (var i = 0; i < Genome.skillCount; i++)
        {
            SkillsTotal[newGenome.skills[i]]++;
            switch (newGenome.skills[i])
            {
                case 0:
                    foodSkill++;
                    col.g += 0.2f;
                    break;
                case 1:
                    attackSkill++;
                    col.r += 0.25f;
                    break;
                case 2:
                    defSkill++;
                    col.b += 0.25f;
                    break;
                case 3:
                    size += 0.5f;
                    break;
            }
        }
        transform.localScale = new Vector3(size, size, size);
        gameObject.GetComponent<SpriteRenderer>().color = col;
        _neuralNetwork = new NeuralNetwork(InputsCount, 8, 4);
        for (int i = 0; i < InputsCount; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                _neuralNetwork.layers[0].weights[i, j] = _genome.weights[i + j * InputsCount];
            }
        }
        for (var i = 0; i < 8; i++)
        {
            for (var j = 0; j < 4; j++)
            {
                _neuralNetwork.layers[1].weights[i, j] = _genome.weights[i + j * 8 + InputsCount * 8];
            }
        }
    }

    private void Kill()
    {
        for (var i = 0; i < Genome.skillCount; i++)
        {
            SkillsTotal[_genome.skills[i]]--;
        }
        Destroy(gameObject);
    }

    private void Eat(float food)
    {
        energy += food;
        if (!(energy > 16)) return;
        energy *= 0.5f;
        var newBacterium = (GameObject)Instantiate(Resources.Load("m1", typeof(GameObject)), new Vector3(0, 0, 0), Quaternion.identity);
        newBacterium.transform.position = transform.position;
        newBacterium.name = "bacterium";
        var newGenome = new Genome(_genome);
        newGenome.Mutate(0.5f);
        var ai = newBacterium.GetComponent<ArtificialIntelligence>();
        ai.Init(newGenome);
        ai.energy = energy;
    }
}
