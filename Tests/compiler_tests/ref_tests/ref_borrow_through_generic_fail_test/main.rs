struct Yo['a]{
    dd: &'a mut i32
}

fn PassAround[T:! type](input: T) -> T {
    input
}

fn main() -> i32 {
    let dd = 5;
    let yo = make Yo {
        dd: &mut dd
    };
    let passed = PassAround(yo);
    dd = 10;
    return dd;
}
