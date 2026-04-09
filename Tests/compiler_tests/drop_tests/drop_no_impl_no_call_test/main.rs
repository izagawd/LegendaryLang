struct NoDrop {
    x: i32
}

fn main() -> i32 {
    let n = make NoDrop { x : 42 };
    n.x
}
