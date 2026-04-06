struct MutHolder['a]{
    r: &'a mut i32
}

fn PassAround[T:! type](input: T) -> T {
    input
}

fn main() -> i32 {
    let x = 5;
    let h = make MutHolder { r: &mut x };
    let h2 = PassAround(h);
    x = 10;
    return x;
}
