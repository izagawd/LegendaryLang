struct Pair {
    x: i32,
    y: i32
}
impl Copy for Pair {}

fn main() -> i32 {
    let b: Gc(Pair) = Gc.New(make Pair { x: 1, y: 2 });
    *b = make Pair { x: 10, y: 32 };
    (*b).x + (*b).y
}
