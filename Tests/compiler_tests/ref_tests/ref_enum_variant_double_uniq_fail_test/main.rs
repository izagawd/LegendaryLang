enum Pair {
    Both(&uniq i32, &uniq i32)
}

fn main() -> i32 {
    let x = 0;
    let p = Pair.Both(&uniq x, &uniq x);
    0
}
