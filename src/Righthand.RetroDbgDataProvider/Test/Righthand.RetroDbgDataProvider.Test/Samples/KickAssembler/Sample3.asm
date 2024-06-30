// Jumps with '*'
        jmp *

        inc $d020
        inc $d021
        jmp *-6

// The same jumps with labels
this:   jmp this

!loop:  inc $d020
        inc $d021
        jmp !loop-