struct Yo['a]{
    dd: &'a uniq i32
}

fn main() -> i32 {
    let a = 5;
    let borrow = make Yo{ dd: &uniq a };
    a
}
