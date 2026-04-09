struct Yo['a]{
    dd: &'a uniq i32
}

fn main() -> i32 {
    let a = 5;
    let b = 10;
    let borrow = make Yo{ dd: &uniq a };
    b
}
