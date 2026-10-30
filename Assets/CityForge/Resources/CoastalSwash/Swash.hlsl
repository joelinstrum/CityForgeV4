// CityForge authored shoreline packet. uv.x is metres inland from the shore;
// uv.y follows the coast. This is an artistic displacement model, not fluid simulation.
float _SwashTime;
float SwashHeight(float2 shore)
{
    float phase=frac(_SwashTime/8.0 + .025*sin(shore.y*.031)+.012*sin(shore.y*.083));
    float advance=smoothstep(0,.63,phase);
    float retreat=smoothstep(.63,1,phase);
    float front=lerp(-15,5.5,advance)-11*retreat;
    front+=.8*sin(shore.y*.12)+.35*sin(shore.y*.29);
    float ridge=exp(-pow((shore.x-front)/5.0,2));
    float envelope=smoothstep(0,.12,phase)*(1-smoothstep(.82,1,phase));
    return .44*ridge*envelope*smoothstep(-38,-24,shore.x)*(1-smoothstep(10,18,shore.x));
}
