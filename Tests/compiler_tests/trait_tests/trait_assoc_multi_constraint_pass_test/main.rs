trait Transformer {
    type Input;
    type Output;
    fn transform(input: Self) -> i32;
}

impl Transformer for i32 {
    type Input = bool;
    type Output = i32;
    fn transform(input: i32) -> i32 {
        input
    }
}

fn use_transformer(T:! Transformer(Input = bool, Output = i32), val: T) -> i32 {
    T.transform(val)
}

fn main() -> i32 {
    use_transformer(i32, 99)
}
