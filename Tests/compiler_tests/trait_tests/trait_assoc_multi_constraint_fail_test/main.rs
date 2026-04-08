trait Transformer: Sized {
    let Input :! type;
    let Output :! type;
    fn transform(input: Self) -> i32;
}

impl Transformer for i32 {
    let Input :! type = bool;
    let Output :! type = i32;
    fn transform(input: i32) -> i32 {
        input
    }
}

fn use_transformer(T:! Transformer(Input = i32, Output = i32), val: T) -> i32 {
    T.transform(val)
}

fn main() -> i32 {
    use_transformer(i32, 5)
}
