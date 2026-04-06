struct UniqHolder['a]{
    r: &'a uniq i32
}

fn PassAround[T:! type](input: T) -> T {
    input
}

fn main() -> i32 {
    let x = 5;
    let h = make UniqHolder { r: &uniq x };
    let h2 = PassAround(h);
    x = 10;
    return x;
}
