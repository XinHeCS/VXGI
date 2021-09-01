
#ifndef _INTERAVG_
#define _INTERAVG_

float4 convRGBA8ToVec4( uint val) {
    return float4 (
        float (( val & 0x000000FF)),
        float (( val & 0x0000FF00) >> 8U),
        float (( val & 0x00FF0000) >> 16U),
        float (( val & 0xFF000000) >> 24U)
    );
}
uint convVec4ToRGBA8( float4 val) {
    return (uint(val.w) & 0x000000FF) << 24U |
        (uint( val .z) & 0x000000FF) << 16U |
        (uint( val.y) & 0x000000FF) << 8U |
        (uint( val.x) & 0x000000FF);
}

void InterlockedRGBA8Avg(RWTexture3D<uint> imgUI, uint3 coord,  float4 val) {
    val.rgb *= 255.0f; // Optimise following calculations
    uint newVal = convVec4ToRGBA8(val);
    uint prevStoredVal = 0;
    uint curStoredVal;
    // Loop as long as destination value gets changed by other threads

    uint loopCount = 0;
    [allow_uav_condition]
    while (true) {
        InterlockedCompareExchange(imgUI[coord], prevStoredVal, newVal, curStoredVal);

        if (prevStoredVal == curStoredVal || loopCount > 255)
        {
            break;
        }
        
        prevStoredVal = curStoredVal;
        float4 rval = convRGBA8ToVec4(curStoredVal);
        rval.xyz = (rval.xyz * rval.w); // Denormalize
        float4 curValF = rval + val; // Add new value
        curValF.xyz /= (curValF.w); // Renormalize
        newVal = convVec4ToRGBA8(curValF);

        ++loopCount;
    }
}


#endif