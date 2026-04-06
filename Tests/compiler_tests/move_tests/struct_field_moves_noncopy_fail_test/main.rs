use Std.Mem.ManuallyDrop;

fn main() -> i32 {
    let made = Box.New(4);
    let manually = make ManuallyDrop { val: made };
    let foo = made;
    5
}
