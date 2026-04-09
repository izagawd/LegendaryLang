struct Holder['a] {
    r: &'a uniq i32
}

fn DropIt[T:! type](x: T) {}

fn main() -> i32 {
    let a = 5;
    let h = make Holder { r: &uniq a };
    DropIt(h);
    a
}
