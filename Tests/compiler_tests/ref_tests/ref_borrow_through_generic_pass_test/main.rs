struct Holder['a]{
    r: &'a uniq i32
}

fn PassAround[T:! type](input: T) -> T {
    input
}

fn DropNow[T:! type](input: T) {}

fn main() -> i32 {
    let x = 5;
    let h = make Holder { r: &uniq x };
    let h2 = PassAround(h);
    DropNow(h2);
    x
}
