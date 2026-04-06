struct Holder['a] {
    val: &'a uniq i32
}

fn main() -> i32 {
    let x = 10;
    let h = make Holder { val: &uniq x };
    x
}
