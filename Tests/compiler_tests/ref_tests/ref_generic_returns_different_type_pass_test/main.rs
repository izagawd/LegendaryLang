struct Holder['a] {
    r: &'a uniq i32
}

fn Consume[T:! type](x: T) -> i32 {
    42
}

fn main() -> i32 {
    let a = 5;
    let h = make Holder { r: &uniq a };
    let result = Consume(h);
    a
}
