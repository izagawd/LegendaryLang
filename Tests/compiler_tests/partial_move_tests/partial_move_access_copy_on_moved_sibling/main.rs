struct Mixed { nc: Box(i32), c: i32 }
fn main() -> i32 {
    let m = make Mixed { nc: Box.New(0), c: 42 };
    let moved = m.nc;
    m.c
}
