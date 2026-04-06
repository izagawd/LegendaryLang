struct Pair {
    x: i32,
    y: i32
}

impl Copy for Pair {}

fn main() -> i32 {
    let b: Box(Pair) = Box.New(make Pair { x: 10, y: 32 });
    let p: Pair = *b;
    p.x + p.y
}
