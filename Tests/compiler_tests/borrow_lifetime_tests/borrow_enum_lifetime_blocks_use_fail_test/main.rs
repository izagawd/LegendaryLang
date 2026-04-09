struct Holder['a] {
    val: &'a uniq i32
}

enum Maybe['a] {
    Some(Holder['a]),
    None
}

fn DropNow[T:! type](input: T) {}

fn main() -> i32 {
    let x = 10;
    let m: Maybe = Maybe.Some(make Holder { val: &uniq x });
    let y = x;
    DropNow(m);
    y
}
