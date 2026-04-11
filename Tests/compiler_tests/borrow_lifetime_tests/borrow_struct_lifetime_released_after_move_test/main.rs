struct Holder['a] {
    val: &'a mut i32
}

fn consume[T:! type](input: T) -> i32 { 42 }

fn main() -> i32 {
    let x = 10;
    let h = make Holder { val: &mut x };
    consume(h);
    x
}
