struct Mixed { nc: Gc(i32), c: i32 }
fn main() -> i32 {
    let m = make Mixed { nc: Gc.New(0), c: 42 };
    let moved = m.nc;
    m.c
}
