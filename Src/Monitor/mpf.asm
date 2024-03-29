;*********************************************************
;*                                                       *
;*    COPYRIGHT , MULTITECH INDUSTRIAL CORP. 1981        *
;*    All right reserved.                                *
;*    No part of this software may be wopied without     *
;*    the express written consent of MULTITECH           *
;*    INDUSTRIAL CORP.                                   *
;*                                                       *
;*********************************************************

P8255:    equ    03h    ;8255 I control port
DIGIT:    equ    02h    ;8255 I port C
SEG7:     equ    01h    ;8255 I port B
KIN:      equ    00h    ;8255 I port A
PWCODE:   equ    A5h    ;Power-up code
ZSUM:     equ    71h    ;This will make the sum of all
                        ;monitor codes to be zero.

COLDEL:   equ    201                        
F1KHZ:    equ    65                        
F2KHZ:    equ    31                        
MPERIOD:  equ    42                        
ONE_1K:   equ    4
ONE_2K:   equ    4
ZERO_1K:  equ    2
ZERO_2K:  equ    8

org 0000h
    ld b,00h
    djnz $
    ld a,90h
    out (P8255),a
    ld a,c0h
    out (DIGIT),a
    ld sp,SYSSTK
    ld a,(POWERUP)
    cp a5h
    call nz,INI
    ld hl,1000h
    call RAMCHK
    jr z,PREPC
    ld h,18h

PREPC:
    ld (USERPC),hl
    ld h,00h
    jr RESET1

org 0028h
RST28:
    ex (sp),hl
    dec hl
    ex (sp),hl
    ld (HLTEMP),hl
    jr CONT28

org 0030h
RST30:
    jr NMI

RESET1:
    ld (1fd2h),hl
    jr RESET2
    ld (hl),c

org 0038h
RST38:
    push hl
    ld hl,(1feeh)
    ex (sp),hl
    ret

CONT28:
    ld (ATEMP),a
    ld hl,(BRAD)
    ld a,(BRDA)
    ld (hl),a
    ld a,10000000B ; 80h
    out (02h),a
    ld a,(ATEMP)
    ld hl,(HLTEMP)
    nop
    ret

RESET2:
    ld hl,USERSTK
    ld (USERSP),hl
    xor a
    ld (TEST),a
    ld ix,MPF_I
    jp SETSTO

org 0066h
NMI:
    ld (ATEMP),a
    ld a,10010000B 
    out (P8255),a
    ld a,c0h
    out (DIGIT),a
    ld a,(ATEMP)

RGSAVE:
    ld (HLTEMP),hl
    pop hl
    ld (ADSAVE),hl
    ld (USERPC),hl
    ld hl,(HLTEMP)
    ld (USERSP),sp
    ld sp,USERIY+2
    push iy
    push ix
    exx
    push hl
    push de
    push bc
    exx
    ex af,af'
    push af
    ex af,af'
    push hl
    push de
    push bc
    push af
    ld a,i
    ld (USERIF+1),a
    ld a,00h
    jp po,SETIF
    ld a,01h

SETIF:
    ld (USERIF),a
    ld sp,SYSSTK
    ld hl,(USERSP)
    ld ix,ERR_SP
    dec hl
    call RAMCHK
    jr nz,SETSTO
    dec hl
    call RAMCHK
    jr nz,SETSTO
    ld ix,SYS_SP
    nop
    nop
    ld de,-USERSTK+1
    add hl,de
    jr c,SETSTO
    ld ix,DISPBF
    scf
    jr BRRSTO

SETSTO:
    xor a
    ld (STATE),a

BRRSTO:
    ld a,(BRDA)
    ld hl,(BRAD)
    ld (hl),a
    call c,MEMDP2

MAIN:
    ld sp,STEPBF
    call SCAN
    call BEEP
    jr MAIN

KEYEXEC:
    cp 10h
    jr c,KHEX
    ld hl,TEST
    set 0,(hl)
    sub a,10h
    cp 08h
    ld hl,KSUBFUN
    jp c,BRANCH
    ld ix,DISPBF
    sub a,08h
    ld hl,STATE
    ld (hl),a
    ld hl,STMINOR
    ld (hl),00h
    ld hl,KFUN
    jp BRANCH

KHEX:
    ld c,a
    ld hl,HTAB

BR1:
    ld a,(STATE)
    jp BRANCH

KINC:
    ld hl,ITAB
    jr BR1

KDEC:
    ld hl,DTAB
    jr BR1

KGO:
    ld hl,GTAB
    jr BR1

KSTEP:
    call TESTM
    jp nz,IGNORE
    ld a,80h
    jp PREOUT

KDATA:
    call TESTM
    jr nz,TESTRG
    call MEMDP2
    ret

TESTRG:
    cp 08h
    jp c,IGNORE
    call REGDP9
    ret

KSBR:
    call TESTM
    jp nz,IGNORE
    ld hl,(ADSAVE)
    call RAMCHK
    jp nz,IGNORE
    ld (BRAD),hl
    call MEMDP2
    ret

KINS:
    call TESTM
    jp nz,IGNORE
    ld hl,(ADSAVE)
    nop
    ld (STEPBF),hl
    inc hl
    ld (STEPBF+4),hl
    call RAMCHK
    jp nz,IGNORE
    ld de,1dfeh
    ld a,h
    cp 1eh
    jr c,SKIPH1
    cp 20h
    jp c,IGNORE
    ld d,27h

SKIPH1:
    ld (STEPBF+2),de

DOMV:
    call GMV
    xor a
    ld (de),a
    ld hl,(STEPBF+4)
    ld (ADSAVE),hl
    call MEMDP2
    ret

KDEL:
    call TESTM
    jp nz,IGNORE
    ld hl,(ADSAVE)
    nop
    ld (STEPBF+4),hl
    call RAMCHK
    jp nz,IGNORE
    ld de,1e00h
    ld a,h
    cp 1eh
    jr c,SKIPH2
    cp 20h
    jp c,IGNORE
    ld d,28h

SKIPH2:
    ld (STEPBF+2),de
    inc hl
    ld (STEPBF),hl
    jr DOMV

KPC:
    ld hl,(USERPC)
    ld (ADSAVE),hl
    call MEMDP2
    ret

KCBR:
    call CLRBR
    ld (ADSAVE),hl
    call MEMDP2
    ret

KREG:
    ld ix,REG_
    call FCONV
    ret

KADDR:
    call MEMDP1
    ret

KMV:
KRL:
    ld hl,(ADSAVE)
    ld (STEPBF),hl

KWT:
KRT:
    call STEPDP
    ret

HFIX:
    jp IGNORE

HDA:
    ld hl,(ADSAVE)
    call RAMCHK
    jp nz,IGNORE
    call PRECL1
    ld a,c
    rld
    call MEMDP2
    ret

HAD:
    ld hl,ADSAVE
    call PRECL2
    ld a,c
    rld
    inc hl
    rld
    call MEMDP1
    ret

HRGAD:
HRGFIX:
    ld a,c
    ld ix,DISPBF
    ld hl,STMINOR
    add a,a
    ld (hl),a
    call REGDP8
    ret

HRT:
HWT:
HRL:
HMV:
    call LOCSTBF
    call PRECL2
    ld a,c
    rld
    inc hl
    rld
    call STEPDP
    ret

HRGDA:
    call LOCRGBF
    call PRECL1
    ld a,c
    rld
    call REGDP9
    ret

IFIX:
IRGFIX:
    jp IGNORE

IAD:
IDA:
    ld hl,(ADSAVE)
    inc hl
    ld (ADSAVE),hl
    call MEMDP2
    ret

IRT:
IWT:
IRL:
IMV:
    ld hl,STMINOR
    inc (hl)
    call LOCSTNA
    jr nz,ISTEP
    dec (hl)
    jp IGNORE

ISTEP:
    call STEPDP
    ret

IRGAD:
IRGDA:
    ld hl,STMINOR
    inc (hl)
    ld a,1fh
    cp (hl)
    jr nc,IRGNA
    ld (hl),00h

IRGNA:
    call REGDP9
    ret

DFIX:
DRGFIX:
    jp IGNORE

DAD:
DDA:
    ld hl,(ADSAVE)
    dec hl
    ld (ADSAVE),hl
    call MEMDP2
    ret

DRT:
DWT:
DRL:
DMV:
    ld hl,STMINOR
    dec (hl)
    call LOCSTNA
    jr nz,DSTEP
    inc (hl)
    jp IGNORE

DSTEP:
    call STEPDP
    ret

DRGAD:
DRGDA:
    ld hl,STMINOR
    dec (hl)
    ld a,1fh
    cp (hl)
    jr nc,DRGNA
    ld (hl),1fh

DRGNA:
    call REGDP9
    ret

GFIX:
GRGFIX:
GRGAD:
GRGDA:
    jp IGNORE

GAD:
GDA:
    ld hl,(BRAD)
    ld (hl),efh
    ld a,ffh

PREOUT:
    ld (TEMP),a
    ld a,(USERIF)
    bit 0,a
    ld hl,c9fbh
    jr nz,EIDI
    ld l,f3h

EIDI:
    ld (TEMP+1),hl
    ld sp,1fbch
    pop af
    pop bc
    pop de
    pop hl
    ex af,af'
    pop af
    ex af,af'
    exx
    pop bc
    pop de
    pop hl
    exx
    pop ix
    pop iy
    ld sp,(USERSP)
    ld (USERAF+1),a
    ld a,(USERIF+1)
    ld i,a
    push hl
    ld hl,(ADSAVE)
    ex (sp),hl
    ld a,(TEMP)
    out (DIGIT),a
    ld a,(USERAF+1)
    jp TEMP+1

GMV:
    ld hl,STEPBF
    call GETP
    jr c,ERROR
    ld de,(STEPBF+4)
    sbc hl,de
    jr nc,MVUP
    ex de,hl
    add hl,bc
    dec hl
    ex de,hl
    ld hl,(STEPBF+2)
    lddr
    inc de
    jr ENDFUN

MVUP:
    add hl,de
    ldir
    dec de
    jr ENDFUN

GRL:
    ld de,(STEPBF)
    inc de
    inc de
    ld hl,(STEPBF+2)
    or a
    sbc hl,de
    ld a,l
    rla
    ld a,h
    adc a,00h
    jr nz,ERROR
    ld a,l
    dec de
    ld (de),a

ENDFUN:
    ld (ADSAVE),de
    call MEMDP2
    ret

GWT:
    call SUM1
    jr c,ERROR
    ld (STEPBF+6),a
    ld hl,4000
    call TONE1K
    ld hl,STEPBF
    ld bc,0007h
    call TAPEOUT
    ld hl,4000
    call TONE2K
    call GETPTR
    call TAPEOUT
    ld hl,4000
    call TONE2K

ENDTAPE:
    ld de,(STEPBF+4)
    jr ENDFUN

ERROR:
    ld ix,ERR_
    jp SETSTO

GRT:
    ld hl,(STEPBF)
    ld (TEMP),hl

LEAD:
    ld a,01000000B
    out (SEG7),a
    ld hl,1000

LEAD1:
    call PERIOD
    jr c,LEAD
    dec hl
    ld a,h
    or l
    jr nz,LEAD1

LEAD2:
    call PERIOD
    jr nc,LEAD2
    ld hl,STEPBF
    ld bc,0007h
    call TAPEIN
    jr c,LEAD
    ld de,(STEPBF)
    call ADDRDP
    ld b,96h

FILEDP:
    call SCAN1
    djnz FILEDP
    ld hl,(TEMP)
    or a
    sbc hl,de
    jr nz,LEAD
    ld a,02h
    out (01h),a
    call GETPTR
    jr c,ERROR
    call TAPEIN
    jr c,ERROR
    call SUM1
    ld hl,STEPBF+6
    cp (hl)
    jr nz,ERROR
    jr ENDTAPE

BRANCH:
    ld e,(hl)
    inc hl
    ld d,(hl)
    inc hl
    add a,l
    ld l,a
    ld l,(hl)
    ld h,00h
    add hl,de
    jp (hl)

IGNORE:
    ld hl,TEST
    set 7,(hl)
    ret

INI:
    ld ix,BLANK
    ld c,07h

INI1:
    ld b,10h

INI2:
    call SCAN1
    djnz INI2
    dec ix
    dec c
    jr nz,INI1
    ld a,PWCODE
    jp INI3

INI4:
    ld hl,NMI
    ld (IM1AD),hl

CLRBR:
    ld hl,ffffh
    ld (BRAD),hl
    ret

TESTM:
    ld a,(STATE)
    cp 01h
    ret z
    cp 02h
    ret

PRECL1:
    ld a,(TEST)
    or a
    ret z
    ld a,00h
    ld (hl),a
    ld (TEST),a
    ret

PRECL2:
    call PRECL1
    ret z
    inc hl
    ld (hl),a
    dec hl
    ret

MEMDP1:
    ld a,01h
    ld b,04h
    ld hl,DISPBF+2
    jr SAV12

MEMDP2:
    ld a,02h
    ld b,02h
    ld hl,DISPBF

SAV12:
    ld (STATE),a
    exx
    ld de,(ADSAVE)
    call ADDRDP
    ld a,(de)
    call DATADP

BRTEST:
    ld hl,(BRAD)
    ld a,(hl)
    ld (BRDA),a
    or a
    sbc hl,de
    jr nz,SETPT1
    ld b,06h
    ld hl,DISPBF
    exx

SETPT1:
    exx

SETPT:
    set 6,(hl)
    inc hl
    djnz SETPT
    ret

STEPDP:
    call LOCSTBF
    ld e,(hl)
    inc hl
    ld d,(hl)
    call ADDRDP
    ld hl,DISPBF+2
    ld b,04h
    call SETPT
    call LOCSTNA
    ld l,a
    ld h,02h
    ld (DISPBF),hl
    ret

LOCSTBF:
    ld a,(STMINOR)
    add a,a
    ld hl,STEPBF
    add a,l
    ld l,a
    ret

LOCSTNA:
    ld a,(STATE)
    sub a,04h
    add a,a
    add a,a
    ld de,STEPTAB
    add a,e
    ld e,a
    ld a,(STMINOR)
    add a,e
    ld e,a
    ld a,(de)
    or a
    ret

REGDP8:
    ld a,08h
    jr RGSTIN

REGDP9:
    ld a,09h

RGSTIN:
    ld (STATE),a
    ld a,(STMINOR)
    res 0,a
    ld b,a
    call RGNADP
    ld a,b
    call LOCRG
    ld e,(hl)
    inc hl
    ld d,(hl)
    ld (ADSAVE),de
    call ADDRDP
    ld a,(STATE)
    cp 09h
    ret nz
    ld hl,DISPBF+2
    ld a,(STMINOR)
    bit 0,a
    jr z,LOCPT
    inc hl
    inc hl

LOCPT:
    set 6,(hl)
    inc hl
    set 6,(hl)
    call FCONV
    ret

RGNADP:
    ld hl,RGTAB
    add a,l
    ld l,a
    ld e,(hl)
    inc hl
    ld d,(hl)
    ld (DISPBF),de
    ret

LOCRGBF:
    ld a,(STMINOR)

LOCRG:
    ld hl,1fbch
    add a,l
    ld l,a
    ret

FCONV:
    ld a,(STMINOR)
    or a
    rra
    cp 0bh
    jr z,FLAGX
    ld c,a
    ld hl,USERIF
    ld a,(hl)
    and 00000001B ; 01h
    ld (hl),a
    ld a,c

FLAGX:
    cp 0ch
    jr nc,FCONV2

FCONV1:
    ld a,(USERAF)
    call DECODE
    ld (FLAGH),hl
    call DECODE
    ld (FLAGL),hl
    ld a,(UAFP)
    call DECODE
    ld (FLAGHP),hl
    call DECODE
    ld (FLAGLP),hl
    ret

FCONV2:
    ld hl,(FLAGH)
    call ENCODE
    ld hl,(FLAGL)
    call ENCODE
    ld (USERAF),a
    ld hl,(FLAGHP)
    call ENCODE
    ld hl,(FLAGLP)
    call ENCODE
    ld (UAFP),a
    ret

DECODE:
    ld b,04h

DRL4:
    add hl,hl
    add hl,hl
    add hl,hl
    rlca
    adc hl,hl
    djnz DRL4
    ret

ENCODE:
    ld b,04h

ERL4:
    add hl,hl
    add hl,hl
    add hl,hl
    add hl,hl
    rla
    djnz ERL4
    ret

SUM1:
    call GETPTR
    ret c

SUM:
    xor a

SUMCAL:
    add a,(hl)
    cpi
    jp pe,SUMCAL
    or a
    ret

GETPTR:
    ld hl,STEPBF+2

GETP:
    ld e,(hl)
    inc hl
    ld d,(hl)
    inc hl
    ld c,(hl)
    inc hl
    ld h,(hl)
    ld l,c
    or a
    sbc hl,de
    ld c,l
    ld b,h
    inc bc
    ex de,hl
    ret

TAPEIN:
    xor a
    ex af,af'

TLOOP:
    call GETBYTE
    ld (hl),e
    cpi
    jp pe,TLOOP
    ex af,af'
    ret

GETBYTE:
    call GETBIT
    ld d,08h

BLOOP:
    call GETBIT
    rr e
    dec d
    jr nz,BLOOP
    call GETBIT
    ret

GETBIT:
    exx
    ld hl,00h

COUNT:
    call PERIOD
    inc d
    dec d
    jr nz,TERR
    jr c,SHORTP
    dec l
    dec l
    set 0,h
    jr COUNT

SHORTP:
    inc l
    bit 0,h
    jr z,COUNT
    rl l
    exx
    ret

TERR:
    ex af,af'
    scf
    ex af,af'
    exx
    ret

PERIOD:
    ld de,00h

LOOPH:
    in a,(KIN)
    inc de
    rla
    jr c,LOOPH
    ld a,11111111B
    out (02h),a

LOOPL:
    in a,(KIN)
    inc de
    rla
    jr nc,LOOPL
    ld a,01111111B
    out (02h),a
    ld a,e
    cp MPERIOD
    ret

TAPEOUT:
    ld e,(hl)
    call OUTBYTE
    cpi
    jp pe,TAPEOUT
    ret

OUTBYTE:
    ld d,08h
    or a
    call OUTBIT

OLOOP:
    rr e
    call OUTBIT
    dec d
    jr nz,OLOOP
    scf
    call OUTBIT
    ret

OUTBIT:
    exx
    ld h,00h
    jr c,OUT1

OUT0:
    ld l,ZERO_2K
    call TONE2K
    ld l,ZERO_1K
    jr BITEND

OUT1:
    ld l,ONE_2K
    call TONE2K
    ld l,ONE_1K

BITEND:
    call TONE1K
    exx
    ret

TONE1K:
    ld c,F1KHZ
    jr TONE

TONE2K:
    ld c,F2KHZ

TONE:
    add hl,hl
    ld de,00h+1
    ld a,ffh

SQWAVE:
    out (02h),a
    ld b,c
    djnz $
    xor 80h
    sbc hl,de
    jr nz,SQWAVE
    ret

RAMCHK:
    ld a,(hl)
    cpl
    ld (hl),a
    ld a,(hl)
    cpl
    ld (hl),a
    cp (hl)
    ret

SCAN:
    push ix
    ld hl,TEST
    bit 7,(hl)
    jr z,SCPRE
    ld ix,BLANK

SCPRE:
    ld b,04h

SCNX:
    call SCAN1
    jr nc,SCPRE
    djnz SCNX
    res 7,(hl)
    pop ix

SCLOOP:
    call SCAN1
    jr c,SCLOOP

KEYMAP:
    ld hl,KEYTAB
    add a,l
    ld l,a
    ld a,(hl)
    ret

SCAN1:
    scf
    ex af,af'
    exx
    ld c,00h
    ld e,11000001B
    ld h,06h

KCOL:
    ld a,e
    out (DIGIT),a
    ld a,(ix+00h)
    out (SEG7),a
    ld b,COLDEL
    djnz $
    xor a
    out (SEG7),a
    ld a,e
    cpl
    or c0h
    out (DIGIT),a
    ld b,06h
    in a,(KIN)
    ld d,a

KROW:
    rr d
    jr c,NOKEY
    ld a,c
    ex af,af'

NOKEY:
    inc c
    djnz KROW
    inc ix
    ld a,e
    and 3fh
    rlc a
    or c0h
    ld e,a
    dec h
    jr nz,KCOL
    ld de,fffah
    add ix,de
    exx
    ex af,af'
    ret

ADDRDP:
    ld hl,DISPBF+2
    ld a,e
    call HEX7SG
    ld a,d
    call HEX7SG
    ret

DATADP:
    ld hl,DISPBF
    call HEX7SG
    ret

HEX7SG:
    push af
    call HEX7
    ld (hl),a
    inc hl
    pop af
    rrca
    rrca
    rrca
    rrca
    call HEX7
    ld (hl),a
    inc hl
    ret

HEX7:
    push hl
    ld hl,SEGTAB
    and 0fh
    add a,l
    ld l,a
    ld a,(hl)
    pop hl
    ret

RAMTEST:
    ld hl,1800h
    ld bc,0800h

RAMT:
    call RAMCHK
    jr z,TNEXT
    halt

TNEXT:
    cpi
    jp pe,RAMT
    rst 0

ROMTEST:
    ld hl,00h
    ld bc,0800h
    call SUM
    jr z,SUMOK
    halt

SUMOK:
    rst 0

INI3:
    ld (POWERUP),a
    ld a,55h
    ld (BEEPSET),a
    ld a,44h
    ld (FBEEP),a
    ld hl,TBEEP
    ld (hl),2fh
    inc hl
    ld (hl),00h
    jp INI4

BEEP:
    push af
    ld hl,FBEEP
    ld c,(hl)
    ld hl,(TBEEP)
    ld a,(BEEPSET)
    cp 55h
    jr nz,NOTONE
    call TONE

NOTONE:
    pop af
    jp KEYEXEC

org 0737h
KSUBFUN:
        defw     KINC
        defb    -KINC+KINC
        defb    -KINC+KDEC
        defb    -KINC+KGO
        defb    -KINC+KSTEP
        defb    -KINC+KDATA
        defb    -KINC+KSBR
        defb    -KINC+KINS
        defb    -KINC+KDEL
    
KFUN:   defw     KPC
        defb    -KPC+KPC
        defb    -KPC+KADDR
        defb    -KPC+KCBR
        defb    -KPC+KREG
        defb    -KPC+KMV
        defb    -KPC+KRL
        defb    -KPC+KWT
        defb    -KPC+KRT
    
HTAB:   defw     HFIX
        defb    -HFIX+HFIX
        defb    -HFIX+HAD
        defb    -HFIX+HDA
        defb    -HFIX+HRGFIX
        defb    -HFIX+HMV
        defb    -HFIX+HRL
        defb    -HFIX+HWT
        defb    -HFIX+HRT
        defb    -HFIX+HRGAD
        defb    -HFIX+HRGDA

ITAB:   defw     IFIX
        defb    -IFIX+IFIX
        defb    -IFIX+IAD
        defb    -IFIX+IDA
        defb    -IFIX+IRGFIX
        defb    -IFIX+IMV
        defb    -IFIX+IRL
        defb    -IFIX+IWT
        defb    -IFIX+IRT
        defb    -IFIX+IRGAD
        defb    -IFIX+IRGDA

DTAB:   defw     DFIX
        defb    -DFIX+DFIX
        defb    -DFIX+DAD
        defb    -DFIX+DDA
        defb    -DFIX+DRGFIX
        defb    -DFIX+DMV
        defb    -DFIX+DRL
        defb    -DFIX+DWT
        defb    -DFIX+DRT
        defb    -DFIX+DRGAD
        defb    -DFIX+DRGDA

GTAB:   defw     GFIX
        defb    -GFIX+GFIX
        defb    -GFIX+GAD
        defb    -GFIX+GDA
        defb    -GFIX+GRGFIX
        defb    -GFIX+GMV
        defb    -GFIX+GRL
        defb    -GFIX+GWT
        defb    -GFIX+GRT
        defb    -GFIX+GRGAD
        defb    -GFIX+GRGDA

KEYTAB:
K0:     defb    03h        ;HEX_3
K1:     defb    07h        ;HEX_7
K2:     defb    0bh        ;HEX_B
K3:     defb    0fh        ;HEX_F
K4:     defb    20h        ;NOT_USED
K5:     defb    21h        ;NOT_USED
K6:     defb    02h        ;HEX_2
K7:     defb    06h        ;HEX_6
K8:     defb    0ah        ;HEX_A
K9:     defb    0eh        ;HEX_E
K0A:    defb    22h        ;NOT_USED
K0B:    defb    23h        ;NOT_USED
K0C:    defb    01h        ;HEX_1
K0D:    defb    05h        ;HEX_5
K0E:    defb    09h        ;HEX_9
K0F:    defb    0dh        ;HEX_D
K10:    defb    13h        ;STEP
K11:    defb    1fh        ;TAPERD
K12:    defb    00h        ;HEX_0
K13:    defb    04h        ;HEX_4
K14:    defb    08h        ;HEX_8
K15:    defb    0ch        ;HEX_C
K16:    defb    12h        ;GO
K17:    defb    1eh        ;TAPEWR
K18:    defb    1ah        ;CBR
K19:    defb    18h        ;PC
K1A:    defb    1bh        ;REG
K1B:    defb    19h        ;ADDR
K1C:    defb    17h        ;DEL
K1D:    defb    1dh        ;RELA
K1E:    defb    15h        ;SBR
K1F:    defb    11h        ;-
K20:    defb    14h        ;DATA
K21:    defb    10h        ;+
K22:    defb    16h        ;INS
K23:    defb    1ch        ;MOVE

org 079fh
MPF_I:  defb    30h     ;'1'
        defb    02h     ;'-'
        defb    02h     ;'-'
        defb    0fh     ;'F'
        defb    1fh     ;'P'
        defb    a1h     ;'u'

BLANK:  defb    00h
        defb    00h
        defb    00h
        defb    00h

ERR_:   defb    00h
        defb    00h
        defb    03h     ;'R'
        defb    03h     ;'R'
        defb    8fh     ;'E'
        defb    02h     ;'-'

SYS_SP: defb    1fh     ;'P'
        defb    aeh     ;'S'
        defb    02h     ;'-'
        defb    aeh     ;'S'
        defb    b6h     ;'Y'
        defb    aeh     ;'S'

ERR_SP: defb    1fh     ;'P'
        defb    aeh     ;'S'
        defb    02h     ;'-'
        defb    03h     ;'R'
        defb    03h     ;'R'
        defb    8fh     ;'E'
        defb    00h

STEPTAB:defb    aeh     ;'S'
        defb    8fh     ;'E'
        defb    b3h     ;'D'
        defb    00h        
        defb    aeh     ;'S'
        defb    b3h     ;'D'
        defb    00h        
        defb    00h        
        defb    0fh     ;'F'
        defb    aeh     ;'S'
        defb    8fh     ;'E'
        defb    00h        
        defb    0fh     ;'F'
        defb    00h

REG_:   defb    00h
        defb    00h
        defb    02h     ;'-'
        defb    beh     ;'G'
        defb    8fh     ;'E'
        defb    03h     ;'R'

RGTAB:  defw    3f0fh    ;'AF'
        defw    a78dh    ;'BC'
        defw    b38fh    ;'DE'
        defw    3785h    ;'HL'
        defw    3f4fh    ;'AF.'
        defw    a7cdh    ;'BC.'
        defw    b3cfh    ;'DE.'
        defw    37c5h    ;'HL.'
        defw    3007h    ;'IX'
        defw    30b6h    ;'IY'
        defw    ae1fh    ;'SP'
        defw    300fh    ;'IF'
        defw    0f37h    ;'FH'
        defw    0f85h    ;'FL'
        defw    0f77h    ;'FH.'
        defw    0fc5h    ;'FL.'
                                                                        
SEGTAB: defb    bdh     ;'0'
        defb    30h     ;'1'
        defb    9bh     ;'2'
        defb    bah     ;'3'
        defb    36h     ;'4'
        defb    aeh     ;'5'
        defb    afh     ;'6'
        defb    38h     ;'7'
        defb    bfh     ;'8'
        defb    beh     ;'9'
        defb    3fh     ;'A'
        defb    a7h     ;'B'
        defb    8dh     ;'C'
        defb    b3h     ;'D'
        defb    8fh     ;'E'
        defb    0fh     ;'F'

;SYSTEM RAM AREA:
USERSTK:    equ 1f9fh    
org USERSTK
defs        16

SYSSTK:     equ 1fafh    
org SYSSTK
STEPBF:        defs        7
DISPBF:        defs        6
REGBF:    
USERAF:        defs        2
USERBC:        defs        2
USERDE:        defs        2
USERHL:        defs        2
UAFP:          defs        2
UBCP:          defs        2
UDEP:          defs        2
UHLP:          defs        2
USERIX:        defs        2
USERIY:        defs        2
USERSP:        defs        2
USERIF:        defs        2
FLAGH:         defs        2
FLAGL:         defs        2
FLAGHP:        defs        2
FLAGLP:        defs        2
USERPC:        defs        2
ADSAVE:        defs        2
BRAD:          defs        2
BRDA:          defs        1
STMINOR:       defs        1
STATE:         defs        1
POWERUP:       defs        1
TEST:          defs        1                    
ATEMP:         defs        1
HLTEMP:        defs        2
TEMP:          defs        4
IM1AD:         defs        2                    
BEEPSET:       defs        1
FBEEP:         defs        1
TBEEP:         defs        2
end

