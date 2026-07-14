--- demonstrates how to use the InstanceScreenFade class to trigger a fade in/out effect on the screen

local function advance_fade()
    local sf = InstanceScreenFade.Get(0) -- 0 here is the first player index, in case you were doing something in multiplayer
    while sf.FadeIn or sf.FadeToBlack do
        advance()
    end
end

advance_fade()
