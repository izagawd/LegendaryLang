struct Holder['a] {
    val: &'a uniq i32
}

fn DropNow[T:! type](input: T) {}

fn main() -> i32 {
    let x = 10;
    let h = make Holder { val: &uniq x };
    let y = x;
    DropNow(h);
    y
}
