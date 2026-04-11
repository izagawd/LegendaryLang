enum Pair {
    Both(&mut i32, &mut i32)
}

fn main() -> i32 {
    let x = 0;
    let p = Pair.Both(&mut x, &mut x);
    0
}
