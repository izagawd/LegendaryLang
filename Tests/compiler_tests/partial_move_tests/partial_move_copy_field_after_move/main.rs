struct Pair { a: GcMut(i32), b: i32 }
fn main() -> i32 {
    let p = make Pair { a: GcMut.New(0), b: 42 };
    let moved = p.a;
    p.b
}
