struct Holder['a] {
    r: &'a mut i32
}

fn Consume[T:! Sized](x: T) -> i32 {
    42
}

fn main() -> i32 {
    let a = 5;
    let h = make Holder { r: &mut a };
    let result = Consume(h);
    a
}
