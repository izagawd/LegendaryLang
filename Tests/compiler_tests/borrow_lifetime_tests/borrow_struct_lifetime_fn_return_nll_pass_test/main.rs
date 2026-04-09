struct Holder['a] {
    val: &'a uniq i32
}

fn wrap(r: &uniq i32) -> Holder {
    make Holder { val: r }
}

fn DropNow[T:! type](input: T) {}

fn main() -> i32 {
    let x = 10;
    let h = wrap(&uniq x);
    DropNow(h);
    x
}
